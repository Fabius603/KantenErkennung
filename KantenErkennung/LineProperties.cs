using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace KantenErkennung
{
    internal class LineProperties
    {
        public Point LineStart {  get; set; }
        public Point LineEnd { get; set; }
        public double LineTilt { get; set; }

    }
}
