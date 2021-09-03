using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Urban_Simulator
{
    public class urbanModel
    {

        public string name = "Urban Model";
        public Surface precicntSrf;
        public List<Curve> roadNetwork;

        public urbanModel()
        {

        }

    }
}
