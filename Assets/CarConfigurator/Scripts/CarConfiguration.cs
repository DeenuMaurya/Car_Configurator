using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarShowroom
{
    public enum CarVariant { Base, Mid, Top }
    public enum PaintFinish { Glossy, Metallic, Matte }
    public enum SeatPosition { Driver, FrontPassenger, RearLeft, RearRight }

    [Serializable]
    public class CarConfiguration
    {
        public CarVariant variant = CarVariant.Base;
        public Color bodyColor = Color.white;
        public PaintFinish paintFinish = PaintFinish.Glossy;
        public Color seatColor = new Color(0.20f, 0.18f, 0.16f);
        public string upholstery = "Fabric";
        public string wheelStyle = "standard";
        public string grilleStyle = "standard";
        public string dashboardTrim = "standard";
        public string steeringStyle = "standard";
        public bool nightMode;
        public List<string> enabledPartIds = new List<string>();
    }

    [Serializable]
    public class PartEntry
    {
        public string id;
        public GameObject root;
        public bool isExterior = true;
        public CarVariant minimumVariant = CarVariant.Base;
        public bool isOptional;
        public int price;
    }

    [Serializable]
    public class StyleOption
    {
        public string id;
        public GameObject root;
        public CarVariant minimumVariant;
        public int price;
    }
}
