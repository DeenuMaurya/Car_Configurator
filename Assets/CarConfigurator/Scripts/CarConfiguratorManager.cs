using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarShowroom
{
    public class CarConfiguratorManager : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Renderer[] seatRenderers;
        [SerializeField] private string bodyColorProperty = "_BaseColor";
        [SerializeField] private string seatColorProperty = "_BaseColor";
        [SerializeField] private List<PartEntry> parts = new List<PartEntry>();
        [SerializeField] private List<StyleOption> wheels = new List<StyleOption>();
        [SerializeField] private List<StyleOption> grilles = new List<StyleOption>();
        [SerializeField] private List<StyleOption> dashboardTrims = new List<StyleOption>();
        [SerializeField] private List<StyleOption> steeringWheels = new List<StyleOption>();
        [SerializeField] private Material glossyPaint;
        [SerializeField] private Material metallicPaint;
        [SerializeField] private Material mattePaint;
        [SerializeField] private Renderer[] upholsteryRenderers;
        [SerializeField] private Material fabricMaterial;
        [SerializeField] private Material leatherMaterial;
        [SerializeField] private Material premiumLeatherMaterial;
        [SerializeField] private CarConfiguration initialConfiguration = new CarConfiguration();
        [Header("Demo prices only; replace with licensed/local market data")]
        [SerializeField] private int baseVariantPrice = 0;
        [SerializeField] private int midVariantPrice = 0;
        [SerializeField] private int topVariantPrice = 0;
        [SerializeField] private int metallicPaintPrice = 0;
        [SerializeField] private int mattePaintPrice = 0;
        [SerializeField] private int leatherPrice = 0;
        [SerializeField] private int premiumLeatherPrice = 0;

        public CarConfiguration Current { get; private set; }
        public event Action ConfigurationChanged;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            if (block == null) block = new MaterialPropertyBlock();
            Current = JsonUtility.FromJson<CarConfiguration>(JsonUtility.ToJson(initialConfiguration));
            Apply();
        }

        private void Start() => Load();

        public void SetVariant(CarVariant variant) { Current.variant = variant; Apply(); }
        public void SetBodyColor(Color color) { Current.bodyColor = color; Apply(); }
        public void SetSeatColor(Color color) { Current.seatColor = color; Apply(); }
        public void SetUpholstery(string materialName) { Current.upholstery = materialName; Apply(); }
        public void SetPaintFinish(PaintFinish finish) { Current.paintFinish = finish; Apply(); }
        public void ResetToDefaults()
        {
            Current = JsonUtility.FromJson<CarConfiguration>(JsonUtility.ToJson(initialConfiguration));
            if (Current.enabledPartIds == null) Current.enabledPartIds = new List<string>();
            Apply();
            Save();
        }
        public bool SetWheelStyle(string id) => ChooseStyle(wheels, id, s => Current.wheelStyle = s);
        public bool SetGrilleStyle(string id) => ChooseStyle(grilles, id, s => Current.grilleStyle = s);
        public bool SetDashboardTrim(string id) => ChooseStyle(dashboardTrims, id, s => Current.dashboardTrim = s);
        public bool SetSteeringStyle(string id) => ChooseStyle(steeringWheels, id, s => Current.steeringStyle = s);

        private bool ChooseStyle(List<StyleOption> options, string id, Action<string> update)
        {
            StyleOption option = options.Find(o => o != null && string.Equals(o.id, id, StringComparison.OrdinalIgnoreCase));
            if (option == null || !option.root || Current.variant < option.minimumVariant) return false;
            update(option.id);
            Apply();
            Save();
            return true;
        }

        public bool SetPart(string id, bool enabled)
        {
            PartEntry entry = FindPart(id);
            if (entry == null || !entry.root || !entry.isOptional || Current.variant < entry.minimumVariant) return false;
            if (enabled && !Current.enabledPartIds.Contains(entry.id)) Current.enabledPartIds.Add(entry.id);
            if (!enabled) Current.enabledPartIds.Remove(entry.id);
            Apply();
            return true;
        }

        public bool IsExteriorPart(string id)
        {
            PartEntry entry = FindPart(id);
            return entry != null && entry.isExterior;
        }

        public PartEntry FindPart(string id) => parts.Find(p => p != null && string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase));

        public int GetEstimatedPrice()
        {
            int sum = Current.variant == CarVariant.Top ? topVariantPrice :
                Current.variant == CarVariant.Mid ? midVariantPrice : baseVariantPrice;
            sum += Current.paintFinish == PaintFinish.Matte ? mattePaintPrice :
                Current.paintFinish == PaintFinish.Metallic ? metallicPaintPrice : 0;
            if (string.Equals(Current.upholstery, "Leather", StringComparison.OrdinalIgnoreCase)) sum += leatherPrice;
            if (string.Equals(Current.upholstery, "PremiumLeather", StringComparison.OrdinalIgnoreCase)) sum += premiumLeatherPrice;
            foreach (PartEntry entry in parts)
                if (entry != null && entry.isOptional && Current.variant >= entry.minimumVariant &&
                    Current.enabledPartIds.Contains(entry.id)) sum += entry.price;
            sum += SelectedStylePrice(wheels, Current.wheelStyle);
            sum += SelectedStylePrice(grilles, Current.grilleStyle);
            sum += SelectedStylePrice(dashboardTrims, Current.dashboardTrim);
            sum += SelectedStylePrice(steeringWheels, Current.steeringStyle);
            return sum;
        }

        public string GetPriceBreakdown(string currency = "₹")
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            int variantPrice = Current.variant == CarVariant.Top ? topVariantPrice :
                Current.variant == CarVariant.Mid ? midVariantPrice : baseVariantPrice;
            result.AppendLine(Current.variant + " variant: " + currency + variantPrice.ToString("N0"));
            if (Current.paintFinish == PaintFinish.Matte) result.AppendLine("Matte paint: " + currency + mattePaintPrice.ToString("N0"));
            if (Current.paintFinish == PaintFinish.Metallic) result.AppendLine("Metallic paint: " + currency + metallicPaintPrice.ToString("N0"));
            foreach (PartEntry part in parts)
                if (part != null && part.isOptional && Current.variant >= part.minimumVariant && Current.enabledPartIds.Contains(part.id))
                    result.AppendLine(part.id + ": " + currency + part.price.ToString("N0"));
            if (string.Equals(Current.upholstery, "Leather", StringComparison.OrdinalIgnoreCase))
                result.AppendLine("Leather: " + currency + leatherPrice.ToString("N0"));
            if (string.Equals(Current.upholstery, "PremiumLeather", StringComparison.OrdinalIgnoreCase))
                result.AppendLine("Premium leather: " + currency + premiumLeatherPrice.ToString("N0"));
            AppendStylePrice(result, wheels, Current.wheelStyle, currency);
            AppendStylePrice(result, grilles, Current.grilleStyle, currency);
            AppendStylePrice(result, dashboardTrims, Current.dashboardTrim, currency);
            AppendStylePrice(result, steeringWheels, Current.steeringStyle, currency);
            result.Append("Estimated total: ").Append(currency).Append(GetEstimatedPrice().ToString("N0"));
            return result.ToString();
        }

        private void AppendStylePrice(System.Text.StringBuilder builder, List<StyleOption> options, string id, string currency)
        {
            StyleOption option = options.Find(o => o != null && o.id == id && Current.variant >= o.minimumVariant);
            if (option != null && option.price > 0)
                builder.AppendLine(option.id + ": " + currency + option.price.ToString("N0"));
        }

        private int SelectedStylePrice(List<StyleOption> options, string id)
        {
            StyleOption selected = options.Find(o => o != null && o.id == id && Current.variant >= o.minimumVariant);
            return selected == null ? 0 : selected.price;
        }

        public void Apply()
        {
            foreach (PartEntry part in parts)
            {
                if (part.root == null) continue;
                bool available = Current.variant >= part.minimumVariant;
                bool visible = available && (!part.isOptional || Current.enabledPartIds.Contains(part.id));
                part.root.SetActive(visible);
            }
            ApplyStyles(wheels, Current.wheelStyle);
            ApplyStyles(grilles, Current.grilleStyle);
            ApplyStyles(dashboardTrims, Current.dashboardTrim);
            ApplyStyles(steeringWheels, Current.steeringStyle);
            Material selectedPaint = Current.paintFinish == PaintFinish.Matte ? mattePaint :
                Current.paintFinish == PaintFinish.Metallic ? metallicPaint : glossyPaint;
            if (selectedPaint != null) SetMaterial(bodyRenderers, selectedPaint, true);
            Material selectedSeat = string.Equals(Current.upholstery, "PremiumLeather", StringComparison.OrdinalIgnoreCase) ? premiumLeatherMaterial :
                string.Equals(Current.upholstery, "Leather", StringComparison.OrdinalIgnoreCase) ? leatherMaterial : fabricMaterial;
            if (selectedSeat != null) SetMaterial(upholsteryRenderers, selectedSeat);
            ApplyColor(bodyRenderers, bodyColorProperty, Current.bodyColor, true);
            ApplyColor(seatRenderers, seatColorProperty, Current.seatColor);
            ConfigurationChanged?.Invoke();
        }

        private void ApplyStyles(List<StyleOption> options, string selected)
        {
            foreach (var option in options)
                if (option != null && option.root != null)
                    option.root.SetActive(Current.variant >= option.minimumVariant &&
                        string.Equals(option.id, selected, StringComparison.OrdinalIgnoreCase));
        }

        private void SetMaterial(Renderer[] renderers, Material material, bool isBodyPaint = false)
        {
            if (renderers == null || material == null) return;
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                Material[] mats = renderer.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (!isBodyPaint || IsPaintMaterial(mats[i].name))
                    {
                        mats[i] = material;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = mats;
            }
        }

        private void ApplyColor(Renderer[] renderers, string property, Color color, bool isBodyPaint = false)
        {
            if (renderers == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                Material[] mats = renderer.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;
                    if (isBodyPaint && !IsPaintMaterial(mat.name)) continue;
                    string prop = mat.HasProperty(property) ? property :
                        mat.HasProperty("_BaseColor") ? "_BaseColor" :
                        mat.HasProperty("_Color") ? "_Color" : null;
                    if (prop == null) continue;
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(prop, color);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }

        private static bool IsPaintMaterial(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.Contains("body") || n.Contains("paint");
        }

        public void Save()
        {
            PlayerPrefs.SetString("CarConfiguration", JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey("CarConfiguration")) return;
            CarConfiguration loaded = JsonUtility.FromJson<CarConfiguration>(PlayerPrefs.GetString("CarConfiguration"));
            if (loaded == null) return;
            Current = loaded;
            if (Current.enabledPartIds == null) Current.enabledPartIds = new List<string>();
            Apply();
        }
    }
}
