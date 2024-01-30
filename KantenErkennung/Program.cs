using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Drawing.Drawing2D;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Linq;

namespace KantenErkennung
{
    internal class Program
    {
        const int SCALE = 10;
        static void Main(string[] args)
        {
            string pfad = "C:\\Users\\schlieper\\OneDrive - Otto Künnecke GmbH\\Desktop\\DPS-Test-Files\\Kantenerkennung.png";
            Mat image = Cv2.ImRead(pfad);
            Cv2.Blur(image, image, new Size(5, 5));
            Cv2.Threshold(image, image, 200, 255, ThresholdTypes.Binary);
            Mat canny = new Mat();
            Cv2.Canny(image, canny, threshold1: 245, threshold2: 255);
            Cv2.ImShow("tesd", canny);
            Size newSize = new Size(image.Width * SCALE, image.Height * SCALE);
            Cv2.Resize(canny, canny, newSize);

            Point[][] contours;
            contours = Cv2.FindContoursAsArray(canny, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            Point[] Konturen = FilterSimilarPoints(contours[0], 20);

            Point[] approxCurve = GetCornerpoints(Konturen);


            PunkteScaleEntfernen(approxCurve);

            if (approxCurve.Length != 8) 
            {
                Console.WriteLine("Falsche Kontur!");
                Console.WriteLine(approxCurve.Length);
            }


            foreach (Point point in approxCurve)
            {
                Cv2.DrawMarker(image, point, Scalar.Red);
            }

            LineProperties BlendeProperties = GeradeDerBlende(approxCurve, image);
            LineProperties KanteOben, KanteUnten = new LineProperties();
            KartenKanten(out KanteOben, out KanteUnten, approxCurve, BlendeProperties);
            Console.WriteLine($"KanteOben: {KanteOben.LineStart}, {KanteOben.LineEnd}");
            Console.WriteLine($"KanteUnten: {KanteUnten.LineStart}, {KanteUnten.LineEnd}");


            Cv2.Line(image, BlendeProperties.LineStart, BlendeProperties.LineEnd, Scalar.Blue, 3);
            Cv2.Line(image, KanteOben.LineStart, KanteOben.LineEnd, Scalar.Red, 3);
            Cv2.Line(image, KanteUnten.LineStart, KanteUnten.LineEnd, Scalar.Red, 3);

            CalcCardTilt(KanteOben, BlendeProperties);
            CalcCardTilt(KanteUnten, BlendeProperties);
            Console.WriteLine($"KO Grad: {KanteOben.LineTilt}");
            Console.WriteLine($"KU Grad: {KanteUnten.LineTilt}");


            Cv2.ImShow("Bild", image);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();
        }
        
        static Point[] GetCornerpoints(Point[] Konturen)
        {
            double epsilon = 0.005 * Cv2.ArcLength(Konturen, true);

            Point[] approxCurve = Cv2.ApproxPolyDP(Konturen, epsilon, true);

            return approxCurve;
            }


        static LineProperties GeradeDerBlende(Point[] Konturen, Mat image) 
        { 
            Vektor vektor = new Vektor();
            LineProperties BlendeProperties = new LineProperties();
            List<Point> POL = new List<Point>();

            foreach (Point point1 in Konturen)
            {
                foreach (Point point2 in Konturen) 
                {
                    vektor.X = Math.Abs(point1.X - point2.X);
                    vektor.Y = Math.Abs(point1.Y - point2.Y);
                    if (CountPointsOnLine(Konturen, vektor, point1, 0.02, 5) >= 3)
                    {
                        if(!POL.Contains(point1))
                        {
                            POL.Add(point1);
                        }
                    }
                }
            }

            BlendeProperties.LineEnd = POL.Aggregate((p1, p2) => p1.Y > p2.Y ? p1 : p2);
            BlendeProperties.LineStart = POL.Aggregate((p1, p2) => p1.Y < p2.Y ? p1 : p2);

            Console.WriteLine(POL.Count);
            CalculateAngle(BlendeProperties);
            Console.WriteLine($"{BlendeProperties.LineStart}, {BlendeProperties.LineEnd}");
            Console.WriteLine($"Grad: {BlendeProperties.LineTilt}");
            return BlendeProperties;
        }

        static void KartenKanten(out LineProperties KanteOben, out LineProperties KanteUnten, Point[] points, LineProperties blendeProperties)
        {
            KanteOben = new LineProperties();
            KanteUnten = new LineProperties();
            List<Point> pointslefttoblende = new List<Point>();

            foreach (Point point in points)
            {
                if (point.X < blendeProperties.LineStart.X && point.X < blendeProperties.LineEnd.X)
                {
                    pointslefttoblende.Add(point);
                }
            }

            if (pointslefttoblende.Count >= 2)
            {
                pointslefttoblende.Sort((p1, p2) => p1.Y.CompareTo(p2.Y));

                KanteOben.LineStart = blendeProperties.LineStart;
                KanteOben.LineEnd = pointslefttoblende[0];

                KanteUnten.LineStart = blendeProperties.LineEnd;
                KanteUnten.LineEnd = pointslefttoblende[1];
            }
            else
            {
                Console.WriteLine("Not enough points left to blende");
                return;
            }

            CalculateAngle(KanteOben);
            CalculateAngle(KanteUnten);
        }

        static int CountPointsOnLine(Point[] points, Vektor vector, Point p1, double tolerance, int point_tolerance)
        {
            int onLineCount = 0;

            if (vector.X == 0 && vector.Y == 0)
            {
                return 0;
            }

            foreach (Point point in points)
            {
                if (p1 == point)
                {
                    continue;
                }

                double tX = 0;
                double tY = 0;

                if (Math.Abs(vector.X) < point_tolerance && Math.Abs(point.X - p1.X) < point_tolerance)
                {
                    onLineCount++;
                    continue;
                }
                else if (Math.Abs(vector.X) >= point_tolerance)
                {
                    tX = (point.X - p1.X) / vector.X;
                }

                if (Math.Abs(vector.Y) < point_tolerance && Math.Abs(point.Y - p1.Y) < point_tolerance)
                {
                    onLineCount++;
                    continue;
                }
                else if (Math.Abs(vector.Y) >= point_tolerance)
                {
                    tY = (point.Y - p1.Y) / vector.Y;
                }

                if (Math.Abs(Math.Abs(tX) - Math.Abs(tY)) < tolerance)
                {
                    onLineCount++;
                }
            }

            return onLineCount;
        }

        static LineProperties CalcCardTilt(LineProperties line, LineProperties blende)
        {
            line.LineTilt = line.LineTilt + blende.LineTilt + 90;
            return line;
        }

        static LineProperties CalculateAngle(LineProperties lineProperties)
        {
            double deltaX = lineProperties.LineStart.X - lineProperties.LineEnd.X;
            double deltaY = lineProperties.LineStart.Y - lineProperties.LineEnd.Y;

            double angleRad = Math.Atan2(deltaY, deltaX);

            lineProperties.LineTilt = angleRad * (180.0 / Math.PI);

            return lineProperties;
        }
        static Point[] PunkteScaleEntfernen(Point[] Konturen)
        {
            for(int i = 0; i < Konturen.Length; i++)
            {
                Konturen[i].X = Konturen[i].X / SCALE;
                Konturen[i].Y = Konturen[i].Y / SCALE;
            }
            return Konturen;
        }

        static Point[] FilterSimilarPoints(Point[] punkte, int toleranz)
        {
            List<Point> gefiltertePunkte = new List<Point>();

            foreach (Point p1 in punkte)
            {
                bool istÄhnlich = false;

                foreach (Point p2 in gefiltertePunkte)
                {
                    if (IstÄhnlich(p1, p2, toleranz))
                    {
                        istÄhnlich = true;
                        break;
                    }
                }

                if (!istÄhnlich)
                {
                    gefiltertePunkte.Add(p1);
                }
            }

            return gefiltertePunkte.ToArray();
        }

        static bool IstÄhnlich(Point p1, Point p2, int toleranz)
        {
            return Math.Abs(p1.X - p2.X) <= toleranz && Math.Abs(p1.Y - p2.Y) <= toleranz;
        }
    }
    public class Vektor
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}