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

            createBlocks(theUrbanModel);                //Using road network, create blocks

            subdivideBlocks(theUrbanModel, 30, 20);                         //Subdivide blocks into plots

            //instantiateBuildings()                    //Place buildings on each plot

            RhinoApp.WriteLine("The Urban Simulator is complete.", EnglishName);

            RhinoDoc.ActiveDoc.Views.Redraw();
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
            int noIterations = 5;

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

        public bool createBlocks(urbanModel model)
        {

            Brep precinctPolySurface = model.precicntSrf.ToBrep().Faces[0].Split(model.roadNetwork, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            List<Brep> blocks = new List<Brep>();

            foreach(BrepFace itBF in precinctPolySurface.Faces)
            {
                Brep itBlock = itBF.DuplicateFace(false);
                itBlock.Faces.ShrinkFaces();
                blocks.Add(itBlock);
                RhinoDoc.ActiveDoc.Objects.AddBrep(itBlock);
            }

            if(blocks.Count > 0)
            {
                model.blocks = blocks;
                return true;
            }
            else
            {
                return false;                    
            }


        }

        public bool subdivideBlocks(urbanModel model, int minPlotDepth, int maxPlotWidth)
        {
            model.plots = new List<Brep>();

            foreach(Brep itBlock in model.blocks)
            {
                Curve[] borderCrvs = itBlock.DuplicateNakedEdgeCurves(true, false);

                List<Curve> splitLines = new List<Curve>();

                itBlock.Faces[0].SetDomain(0, new Interval(0, 1));
                itBlock.Faces[0].SetDomain(1, new Interval(0, 1));

                Point3d pt1 = itBlock.Surfaces[0].PointAt(0, 0);
                Point3d pt2 = itBlock.Surfaces[0].PointAt(0, 1);
                Point3d pt3 = itBlock.Surfaces[0].PointAt(1, 1);
                Point3d pt4 = itBlock.Surfaces[0].PointAt(1, 0);

                double length = pt1.DistanceTo(pt2);
                double width = pt1.DistanceTo(pt4);

                Point3d sdPt1 = new Point3d();
                Point3d sdPt2 = new Point3d();

                if (length > width) //length is wider
                {
                    if (width > (minPlotDepth * 2)) //Suitable for subdivision
                    {
                        //Create a subdividing line
                        sdPt1 = itBlock.Surfaces[0].PointAt(0.5, 0);
                        sdPt2 = itBlock.Surfaces[0].PointAt(0.5, 1);
                    }
                }
                else //Width is wider
                {
                    if (length > (minPlotDepth * 2)) //Suitable for subdivision
                    {
                        //Create a subdividing line
                        sdPt1 = itBlock.Surfaces[0].PointAt(0, 0.5);
                        sdPt2 = itBlock.Surfaces[0].PointAt(1, 0.5);
                    }

                }

                Line subDLine = new Line(sdPt1, sdPt2);
                Curve subDCrv = subDLine.ToNurbsCurve();

                splitLines.Add(subDCrv);

                double crvLength = subDCrv.GetLength();
                double noPlots = Math.Floor(crvLength / maxPlotWidth);

                for(int t = 0; t < noPlots - 1; t++)
                {
                    double tVal = t * (1 / noPlots);

                    Plane perpFrm;

                    Point3d evalPt = subDCrv.PointAtNormalizedLength(tVal);
                    subDCrv.PerpendicularFrameAt(tVal, out perpFrm);

                    Point3d ptPer2Up = Point3d.Add(evalPt, perpFrm.XAxis);
                    Point3d ptPer2Down = Point3d.Add(evalPt, -perpFrm.XAxis);

                    //draw line perpendicular
                    Line ln1 = new Line(evalPt, ptPer2Up);
                    Line ln2 = new Line(evalPt, ptPer2Down);

                    Curve lnExt1 = ln1.ToNurbsCurve().ExtendByLine(CurveEnd.End, borderCrvs);
                    Curve lnExt2 = ln2.ToNurbsCurve().ExtendByLine(CurveEnd.End, borderCrvs);

                    splitLines.Add(lnExt1);
                    splitLines.Add(lnExt2);
                                       

                }


                Brep plotPolySurface = itBlock.Faces[0].Split(splitLines, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);


                foreach (BrepFace itBF in plotPolySurface.Faces)
                {
                    Brep itPlot = itBF.DuplicateFace(false);
                    itPlot.Faces.ShrinkFaces();
                    model.plots.Add(itBlock);
                    RhinoDoc.ActiveDoc.Objects.AddBrep(itPlot);
                }


                RhinoDoc.ActiveDoc.Views.Redraw();

            }
            
            return true;
        }

    }
}
