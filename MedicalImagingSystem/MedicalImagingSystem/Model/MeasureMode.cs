using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MedicalImagingSystem.Model
{
    public enum MeasureMode
    {
        None,
        Length,
        Angle
    }

    public class MeasureLine
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public double Length { get; set; }
        public double MidX => (Start.X + End.X) / 2;
        public double MidY => (Start.Y + End.Y) / 2;
    }

    public class MeasureAngle
    {
        public Point Point1 { get; set; }
        public Point Vertex { get; set; }
        public Point Point2 { get; set; }
        public double Angle { get; set; }
    }
}