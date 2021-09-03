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

            urbanModel theUrbanModel = new urbanModel();

            if (!getPrecinct(theUrbanModel))            //ask user to select a surface representing the precinct 
                return Result.Failure;

            if (!generateRoadNetwork(theUrbanModel))    //Ui=sing precinct generate road network
                return Result.Failure;

            //createBlocks()                            //Using road network, create blocks
            //subdivideBlocks()                         //Subdivide blocks into plots
            //instantiateBuildings()                    //Place buildings on each plot

            RhinoApp.WriteLine("The Urban Simulator is complete.", EnglishName);


            return Result.Success;
        }

        public bool getPrecinct(urbanModel model)
        {

            GetObject obj = new GetObject();
            obj.GeometryFilter = Rhino.DocObjects.ObjectType.Surface;
            obj.SetCommandPrompt("Please select a Surface representing your Precinct");

            GetResult res = obj.Get();

            if (res != GetResult.Object)
            {
                RhinoApp.WriteLine("User failed to select a surface.", EnglishName);
                return false;

            }


            if (obj.ObjectCount == 1)
                model.precicntSrf = obj.Object(0).Surface();

            return true;
        }

        public bool generateRoadNetwork(urbanModel model)
        {
            int noIterations = 4;

            Random rndRoadT = new Random();

            List<Curve> obstCrvs = new List<Curve>();

            //extract the border from precinct surface
            Curve[] borderCrvs = model.precicntSrf.ToBrep().DuplicateNakedEdgeCurves(true, false);

            foreach (Curve itCrv in borderCrvs)
                obstCrvs.Add(itCrv);

            if (borderCrvs.Length > 0)
            {
                int noBorders = borderCrvs.Length;

                Random rnd = new Random();
                Curve theCrv = borderCrvs[rnd.Next(noBorders)];

                recursivePerpLine(theCrv, ref obstCrvs, rndRoadT, 1, 4);
            }

            model.roadNetwork = obstCrvs;

            if (obstCrvs.Count > borderCrvs.Length)
                return true;
            else
                return false;

            return true;
        }

        public bool recursivePerpLine(Curve inpCrv, ref List<Curve> inpObst, Random inpRnd, int dir, int cnt)
        {
            if (cnt < 1)
                return false;

            //select random point on edges
            double t = inpRnd.Next(20, 80) / 100.0;
            Plane perpFrm;

            Point3d pt = inpCrv.PointAtNormalizedLength(t);
            inpCrv.PerpendicularFrameAt(t, out perpFrm);

            Point3d pt2 = Point3d.Add(pt, perpFrm.XAxis * dir);

            //draw line perpendicular
            Line ln = new Line(pt, pt2);
            Curve lnExt = ln.ToNurbsCurve().ExtendByLine(CurveEnd.End, inpObst);

            if (lnExt == null)
                return false;

            inpObst.Add(lnExt);

            RhinoDoc.ActiveDoc.Objects.AddLine(lnExt.PointAtStart, lnExt.PointAtEnd);
            RhinoDoc.ActiveDoc.Objects.AddPoint(pt);
            RhinoDoc.ActiveDoc.Views.Redraw();

            recursivePerpLine(lnExt, ref inpObst, inpRnd, 1, cnt - 1);
            recursivePerpLine(lnExt, ref inpObst, inpRnd, -1, cnt - 1);

            return true;
        }
    }
}
