using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace Urban_Simulator
{
    public class UrbanSimulatorCommand : Command
    {
        public UrbanSimulatorCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        //testing for git

        ///<summary>The only instance of this command.</summary>
        public static UrbanSimulatorCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "UrbanSimulator"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            RhinoApp.WriteLine("The Urban Simulator has begun.", EnglishName);

            //getprecinct()             //ask user to select a surface representing the precinct
            //generateRoadNetwort()     //Ui=sing precinct generate road network
            //createBlocks()            //Using road network, create blocks
            //subdivideBlocks()         //Subdivide blocks into plots
            //instantiateBuildings()    //Place buildings on each plot

            RhinoApp.WriteLine("The Urban Simulator is complete.", EnglishName);

            return Result.Success;
        }
    }
}
