using System.Collections.Generic;

namespace MvcApplication1.Areas.EngineeringTools.Models.WakeSimulation
{
    public class VWakeSimulation
    {
        public List<VTurbine> Turbines { get; set; }

        public string SolverState { get; set; }

        public string SolverOutputDir { get; set; }

        public decimal GridPointsX { get; set; }
        
        public decimal GridPointsY { get; set; }

        public decimal TurbineDiameter { get; set; }

        public decimal TurbineHeight { get; set; }

        public decimal HubThrust { get; set; }

        public decimal WakeDecay { get; set; }

        public decimal VelocityAtHub { get; set; }

        public int TurbinesAmount { get; set; }
       
        public decimal AirDensity { get; set; }

        public decimal UnknownProperty { get; set; }
       
        public decimal RotationAngle { get; set; }

        public VWakeSimulation()
        {
            Turbines = new List<VTurbine>();
        }
    }
}