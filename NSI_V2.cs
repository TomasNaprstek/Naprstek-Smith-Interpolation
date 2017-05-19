using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NSI_V2
{
    public static class Globals
    {
        //*USER-INPUT VARIABLES
        public static string inputFile = "Sudbury.txt"; //raw data file with the format of X - Y - Value - Line number, tab delimited
        public static string outputFile = "Sudbury-50m250m.txt"; //output file
        public static double cellSize = 200; //edge size of each square cell in metres
        public static double interpDist = 1000; //metres away that will be interpolated
        //public static int totalLoop = 25; //the number of times the interpolation loop will be processed
        public static double searchStepSize = 0.25; //how much of a cell we will "travel" each search step
        //*********************
    }

    public class cellData
    {
        //Properties
        public double[,] X { get; set; }
        public double[,] Y { get; set; }
        public double[,] Value { get; set; }
        public int[,] Flag { get; set; }

        /*
         * 0 = Left
         * 1 = Up left
         * 2 = Up
         * 3 = Up right
         * 4 = Right
         * 5 = Down right
         * 6 = Down
         * 7 = Down left
        */
        //distance to real data cell in 8 directions, close[x,y,0]=Left,distance
        public int[, ,] close { get; set; }

        //x,y location of 1 cell away data in each direction (i.e. already taken into account edges, etc.), close1[x,y,3,0]=UR,x, close1[x,y,3,1]=UR,y
        public int[, , ,] close1 { get; set; }

        //Constructors
        public cellData(double[,] Xint, double[,] Yint, double[,] Valueint, int[,] Flagint, int[, ,] closeInt, int[, , ,] close1Int)
        {
            X = Xint;
            Y = Yint;
            Value = Valueint;
            Flag = Flagint;
            close = closeInt;
            close1 = close1Int;
        }

        public cellData(double[,] Xint, double[,] Yint, double[,] Valueint, int[,] Flagint)
        {
            X = Xint;
            Y = Yint;
            Value = Valueint;
            Flag = Flagint;
        }
    }

    class Program
    {
        static void Main()
        {
            //**********IMPORT GRID
            string temp;

            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            List<double> Value = new List<double>();
            List<double> Line = new List<double>();

            FileStream myfile = new FileStream(Globals.inputFile, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(myfile);
            while (!reader.EndOfStream)
            {
                temp = reader.ReadLine();
                if (temp == "")
                {
                }
                else
                {
                    var delimiters = new char[] { '\t' };
                    var segments = temp.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    int track = 0;
                    foreach (var segment in segments)
                    {
                        if (track == 0)
                        {
                            X.Add(Convert.ToDouble(segment));
                        }
                        else if (track == 1)
                        {
                            Y.Add(Convert.ToDouble(segment));
                        }
                        else if (track == 2)
                        {
                            Value.Add(Convert.ToDouble(segment));
                        }
                        else if (track == 3)
                        {
                            Line.Add(Convert.ToDouble(segment));
                        }
                        track++;
                    }
                }
            }
            myfile.Close();
            //***********************************

            //****************FULL GRID PROCEDURE
            //Find the edges of the total grid area
            int lengthX = Convert.ToInt32(Math.Ceiling(((X.Max() - X.Min()) / Globals.cellSize)));
            int lengthY = Convert.ToInt32(Math.Ceiling(((Y.Max() - Y.Min()) / Globals.cellSize)));

            double[,] tempX = new double[lengthX, lengthY];
            double[,] tempY = new double[lengthX, lengthY];
            double[,] tempV = new double[lengthX, lengthY];
            int[,] tempF = new int[lengthX, lengthY];

            double minX = X.Min();
            double minY = Y.Min();

            for (int k = 0; k < X.Count; k++)
            {
                //ints, will round down, therefore the xPos will be in the grid as using the bottom left point as the reference.
                int xPos = Convert.ToInt32(Math.Floor((X[k] - minX) / Globals.cellSize));
                int yPos = Convert.ToInt32(Math.Floor((Y[k] - minY) / Globals.cellSize));
                tempX[xPos, yPos] = ((xPos * Globals.cellSize) + minX + (Globals.cellSize / 2)); //Essentially round the position to the center of the cell
                tempY[xPos, yPos] = ((yPos * Globals.cellSize) + minY + (Globals.cellSize / 2)); //Essentially round the position to the center of the cell
                tempV[xPos, yPos] += Value[k]; //Add the value of that reading to the cell.
                tempF[xPos, yPos]++; //Account for how many readings have been assigned to the cell.
            }
            //Below code uses parallel processing for the gridding. Can be useful on VERY large data sizes.
            /*
            Parallel.For(0, X.Count, index =>
            {
                //ints, will round down, therefore the xPos will be in the grid as using the bottom left point as the reference.
                int xPos = Convert.ToInt32(Math.Floor((X[index] - minX) / Globals.cellSize));
                int yPos = Convert.ToInt32(Math.Floor((Y[index] - minY) / Globals.cellSize));
                tempX[xPos, yPos] = ((xPos * Globals.cellSize) + minX + (Globals.cellSize / 2)); //Essentially round the position to the center of the cell
                tempY[xPos, yPos] = ((yPos * Globals.cellSize) + minY + (Globals.cellSize / 2)); //Essentially round the position to the center of the cell
                tempV[xPos, yPos] += Value[index]; //Add the value of that reading to the cell.
                tempF[xPos, yPos]++; //Account for how many readings have been assigned to the cell.
            });*/

            int[, ,] tempclose = new int[tempX.GetUpperBound(0) + 1, tempX.GetUpperBound(1) + 1, 8];
            int[, , ,] tempclose1 = new int[tempX.GetUpperBound(0) + 1, tempX.GetUpperBound(1) + 1, 8, 2];

            cellData gridedData = new cellData(tempX, tempY, tempV, tempF, tempclose, tempclose1);

            //Check through all grid cells, and if a cell has more than one reading in it, average the value over the number of readings.
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    if (gridedData.Flag[i, j] > 1)
                    {
                        gridedData.Value[i, j] = gridedData.Value[i, j] / gridedData.Flag[i, j];
                        gridedData.Flag[i, j] = 1; //Set it back to 1, showing it is populated by a "true" value
                    }
                }
            }
            //Now go through all cells, and assign values to the ones that have no data currently, or determine if they are too far away from real data to use. 
            //During this process we will also find all closest data to each cell which will be information needed when normalizing.
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    if (gridedData.Flag[i, j] != 1)
                    {
                        gridedData.X[i, j] = (i * Globals.cellSize) + minX + (Globals.cellSize / 2); //Essentially round the position to the center of the cell
                        gridedData.Y[i, j] = (j * Globals.cellSize) + minY + (Globals.cellSize / 2);
                    }

                    //L,UL,U,UR,R,DR,D,DL
                    int[] xLoop = new int[8] { -1, -1, 0, 1, 1, 1, 0, -1 };
                    int[] yLoop = new int[8] { 0, 1, 1, 1, 0, -1, -1, -1 };
                    bool complete = false;
                    double closeDistVal = 0;
                    double closeDist = Globals.interpDist + 1;
                    int closeDistNum = 0;

                    for (int k = 0; k < 8; k++)
                    {
                        int loopI = 1;
                        complete = false;
                        while (complete == false)
                        {
                            if (i + xLoop[k] * loopI >= lengthX || i + xLoop[k] * loopI < 0) //hit an edge in x-direction
                            {
                                if (loopI == 1)
                                {
                                    gridedData.close1[i, j, k, 0] = i; //set that cell's close1 in that direction to its own position
                                    gridedData.close1[i, j, k, 1] = j;
                                }
                                gridedData.close[i, j, k] = -1;
                                complete = true;
                            }
                            else if (j + yLoop[k] * loopI >= lengthY || j + yLoop[k] * loopI < 0) //hit an edge y-direction
                            {
                                if (loopI == 1)
                                {
                                    gridedData.close1[i, j, k, 0] = i;
                                    gridedData.close1[i, j, k, 1] = j;
                                }
                                gridedData.close[i, j, k] = -1;
                                complete = true;
                            }
                            else if (gridedData.Flag[i + xLoop[k] * loopI, j + yLoop[k] * loopI] == -1) //outside of interpolation distance
                            {
                                if (loopI == 1)
                                {
                                    gridedData.close1[i, j, k, 0] = i;
                                    gridedData.close1[i, j, k, 1] = j;
                                }
                                gridedData.close[i, j, k] = -1;
                                complete = true;
                            }
                            else //everything is OK at this location
                            {
                                if (loopI == 1) //if this is the first loop, indicate cell's x and y
                                {
                                    gridedData.close1[i, j, k, 0] = i + xLoop[k] * loopI;
                                    gridedData.close1[i, j, k, 1] = j + yLoop[k] * loopI;
                                }
                                if (gridedData.Flag[i + xLoop[k] * loopI, j + yLoop[k] * loopI] == 1) //if this location is real data, then indicate distance to it
                                {
                                    gridedData.close[i, j, k] = loopI;
                                    complete = true;
                                    double tempLoopI = loopI * Math.Sqrt(Math.Pow(xLoop[k], 2) + Math.Pow(yLoop[k], 2)); //distance to the current loop location
                                    if (closeDist > tempLoopI) //if this real data point is closer, then we use it
                                    {
                                        closeDistNum = 1;
                                        closeDist = tempLoopI;
                                        closeDistVal = gridedData.Value[i + xLoop[k] * loopI, j + yLoop[k] * loopI];
                                    }
                                    else if (closeDist == tempLoopI) //if this real data point is the same distance away, then we will average over the values
                                    {
                                        closeDistNum++;
                                        closeDist = tempLoopI;
                                        closeDistVal = closeDistVal + gridedData.Value[i + xLoop[k] * loopI, j + yLoop[k] * loopI];
                                    }
                                }
                                else //not real data, therefore must keep looking
                                {
                                    loopI++;
                                    if (loopI > Globals.interpDist / Globals.cellSize) //Outside of the range of interest, therefore not found
                                    {
                                        gridedData.close[i, j, k] = -1;
                                        complete = true;
                                    }
                                }
                            }
                        }
                        if (gridedData.Flag[i, j] == 1) //if this was a real data cell, then we need to re-assign its closest real dats as itself
                        {
                            gridedData.close[i, j, k] = 0;
                        }
                    }

                    if (gridedData.Flag[i, j] != 1) //if this was not a real data cell then assign the values
                    {
                        //Now need to find closest real data and assign its value to the current cell.
                        if (closeDist != Globals.interpDist + 1)
                        {
                            gridedData.Value[i, j] = closeDistVal / closeDistNum;
                            gridedData.Flag[i, j] = 0;
                        }
                        else //no real data was close enough, therefore it is outside of the range.
                        {
                            gridedData.Value[i, j] = -999999;
                            gridedData.Flag[i, j] = -1;
                            for (int k = 0; k < 8; k++)
                            {
                                gridedData.close1[i, j, k, 0] = i;
                                gridedData.close1[i, j, k, 1] = j;
                            }
                        }
                    }

                }
            }
            //***********************************

            //************************ADD MIN VAL

            //Find minimum real value and add to all data to ensure RealReplaceV2 works properly
            double minVal = 10000000;
            double maxVal = 0;
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    if (gridedData.Flag[i, j] == 1)
                    {
                        if (gridedData.Value[i, j] < minVal)
                        {
                            minVal = gridedData.Value[i, j];
                        }
                        if (gridedData.Value[i, j] > maxVal)
                        {
                            maxVal = gridedData.Value[i, j];
                        }
                    }
                }
            }

            if (minVal < 0)
            {
                //once found, add this amount to each data point
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedData.Flag[i, j] != -1)
                        {
                            gridedData.Value[i, j] = gridedData.Value[i, j] + Math.Abs(minVal);
                        }
                    }
                }
                maxVal = maxVal + Math.Abs(minVal); //also add the minVal to the maxVal so can use it properly later on
            }
            //***********************************

            //*****************ALPHA-TRIMMED MEAN
            for (int loop2 = 0; loop2 < 1; loop2++)
            {
                double[,] tempX4 = new double[lengthX, lengthY];
                double[,] tempY4 = new double[lengthX, lengthY];
                double[,] tempV4 = new double[lengthX, lengthY];
                int[,] tempF4 = new int[lengthX, lengthY];

                cellData alphaData = new cellData(tempX4, tempY4, tempV4, tempF4);

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        double tempGX = gridedData.X[i, j];
                        alphaData.X[i, j] = tempGX;
                        double tempGY = gridedData.Y[i, j];
                        alphaData.Y[i, j] = tempGY;
                        int tempGF = gridedData.Flag[i, j];
                        alphaData.Flag[i, j] = tempGF;
                    }
                }

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedData.Flag[i, j] == -1) //If the data is outside of the grid, then don't use it
                        {
                        }
                        else
                        {
                            List<double> points = new List<double>();
                            points.Add(gridedData.Value[gridedData.close1[i, j, 0, 0], gridedData.close1[i, j, 0, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 1, 0], gridedData.close1[i, j, 1, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 2, 0], gridedData.close1[i, j, 2, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 3, 0], gridedData.close1[i, j, 3, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 4, 0], gridedData.close1[i, j, 4, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 5, 0], gridedData.close1[i, j, 5, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 6, 0], gridedData.close1[i, j, 6, 1]]);
                            points.Add(gridedData.Value[gridedData.close1[i, j, 7, 0], gridedData.close1[i, j, 7, 1]]);
                            points.Add(gridedData.Value[i, j]);

                            //Make sure no data that is outside of the grid is being referenced
                            for (int k = 0; k < 9; k++)
                            {
                                if (points[k] == -999999)
                                {
                                    points[k] = gridedData.Value[i, j];
                                    gridedData.close1[i, j, k, 0] = i;
                                    gridedData.close1[i, j, k, 1] = j;
                                }
                            }

                            points.Sort();
                            int numAlpha = 0;
                            double sumAlpha = 0;
                            for (int k = 2; k < points.Count - 2; k++)
                            {
                                sumAlpha = sumAlpha + points[k];
                                numAlpha++;
                            }
                            alphaData.Value[i, j] = sumAlpha / numAlpha;
                            double difference = alphaData.Value[i, j] - gridedData.Value[i, j];
                            double maxV = Math.Ceiling((Globals.interpDist / Globals.cellSize) / 2);
                            double distR = maxV * 2;
                            if (gridedData.Flag[i, j] == 1)
                            {
                                distR = 0;
                            }
                            else
                            {
                                List<double> points2 = new List<double>();
                                points2.Add(gridedData.close[i, j, 0]);
                                points2.Add(gridedData.close[i, j, 1]);
                                points2.Add(gridedData.close[i, j, 2]);
                                points2.Add(gridedData.close[i, j, 3]);
                                points2.Add(gridedData.close[i, j, 4]);
                                points2.Add(gridedData.close[i, j, 5]);
                                points2.Add(gridedData.close[i, j, 6]);
                                points2.Add(gridedData.close[i, j, 7]);

                                //custom min finder due to -1 values
                                for (int k = 0; k < 8; k++)
                                {
                                    if (points2[k] != -1)
                                    {
                                        if (points2[k] < distR)
                                        {
                                            distR = points2[k];
                                        }
                                    }
                                }
                            }
                            //double newDiff = difference*(1/(1+Math.Exp(-3.5*(distR-(maxV/2)))));
                            if (distR > maxV)
                            {
                                distR = maxV;
                            }
                            double newDiff = difference * (1 / (maxV / distR));
                            if (distR == 0)
                            {
                                newDiff = 0;
                            }
                            gridedData.Value[i, j] = gridedData.Value[i, j] + newDiff;
                        }
                    }
                }
            }
            //***********************************

            //*******************************LOOP

            double[,] tempX2 = new double[lengthX, lengthY];
            double[,] tempY2 = new double[lengthX, lengthY];
            double[,] tempV2 = new double[lengthX, lengthY];
            int[,] tempF2 = new int[lengthX, lengthY];
            double[,] tempX3 = new double[lengthX, lengthY];
            double[,] tempY3 = new double[lengthX, lengthY];
            double[,] tempV3 = new double[lengthX, lengthY];
            int[,] tempF3 = new int[lengthX, lengthY];
            double[,] tempX5 = new double[lengthX, lengthY];
            double[,] tempY5 = new double[lengthX, lengthY];
            double[,] tempV5 = new double[lengthX, lengthY];
            int[,] tempF5 = new int[lengthX, lengthY];

            int[, ,] smallestL = new int[lengthX, lengthY, 4];
            double[, ,] smallestLV = new double[lengthX, lengthY, 4];
            double[, ,] smallestLVOrder = new double[lengthX, lengthY, 4];

            double[,] strength = new double[lengthX, lengthY];

            cellData gridedDataDerivIterate = new cellData(tempX2, tempY2, tempV2, tempF2);
            cellData realReplace = new cellData(tempX3, tempY3, tempV3, tempF3);
            cellData previousGrid = new cellData(tempX5, tempY5, tempV5, tempF5);

            int looping = 0;
            int currentLoop = 0;

            double previousMean = 0;
            double previousMedian = 0;

            int numStops = 0;

            while (looping == 0)
            {
                //****************************DerivV3
                if (currentLoop == 0) //this is the first loop, therefore need the data from gridedData
                {
                    for (int i = 0; i < lengthX; i++)
                    {
                        for (int j = 0; j < lengthY; j++)
                        {
                            double tempGX = gridedData.X[i, j];
                            gridedDataDerivIterate.X[i, j] = tempGX;
                            double tempGY = gridedData.Y[i, j];
                            gridedDataDerivIterate.Y[i, j] = tempGY;
                            double tempGV = gridedData.Value[i, j];
                            gridedDataDerivIterate.Value[i, j] = tempGV;
                            int tempGF = gridedData.Flag[i, j];
                            gridedDataDerivIterate.Flag[i, j] = tempGF;
                        }
                    }
                }
                else //this is NOT the first loop, therefore need the data from realReplace
                {
                    for (int i = 0; i < lengthX; i++)
                    {
                        for (int j = 0; j < lengthY; j++)
                        {
                            double tempGX = realReplace.X[i, j];
                            gridedDataDerivIterate.X[i, j] = tempGX;
                            double tempGY = realReplace.Y[i, j];
                            gridedDataDerivIterate.Y[i, j] = tempGY;
                            double tempGV = realReplace.Value[i, j];
                            gridedDataDerivIterate.Value[i, j] = tempGV;
                            int tempGF = realReplace.Flag[i, j];
                            gridedDataDerivIterate.Flag[i, j] = tempGF;

                            previousGrid.X[i, j] = tempGX;
                            previousGrid.Y[i, j] = tempGY;
                            previousGrid.Value[i, j] = tempGV;
                            previousGrid.Flag[i, j] = tempGF;
                        }
                    }
                }

                double[,] TaylorLoopVals = new double[lengthX, lengthY]; //need a temporary variable that will hold the new values during a full iteration

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        double tempTL = gridedDataDerivIterate.Value[i, j];
                        TaylorLoopVals[i, j] = tempTL;
                    }
                }

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedDataDerivIterate.Flag[i, j] == -1 || gridedDataDerivIterate.Value[i, j] == -999999) //If the data is outside of the grid, then don't use it
                        {
                            gridedDataDerivIterate.Flag[i, j] = -1;
                        }
                        else
                        {
                            double[] points = new double[9]; // 8 points around center point
                            points[0] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 0, 0], gridedData.close1[i, j, 0, 1]];
                            points[1] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 1, 0], gridedData.close1[i, j, 1, 1]];
                            points[2] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 2, 0], gridedData.close1[i, j, 2, 1]];
                            points[3] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 3, 0], gridedData.close1[i, j, 3, 1]];
                            points[4] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 4, 0], gridedData.close1[i, j, 4, 1]];
                            points[5] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 5, 0], gridedData.close1[i, j, 5, 1]];
                            points[6] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 6, 0], gridedData.close1[i, j, 6, 1]];
                            points[7] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 7, 0], gridedData.close1[i, j, 7, 1]];
                            points[8] = gridedDataDerivIterate.Value[i, j];

                            //Make sure no data that is outside of the grid is being referenced
                            for (int k = 0; k < 9; k++)
                            {
                                if (points[k] == -999999)
                                {
                                    points[k] = gridedDataDerivIterate.Value[i, j];
                                    gridedData.close1[i, j, k, 0] = i;
                                    gridedData.close1[i, j, k, 1] = j;
                                }
                            }

                            double fx = (points[4] - points[0]) / (2 * Globals.cellSize);
                            double fy = (points[2] - points[6]) / (2 * Globals.cellSize);
                            double fxx = (points[4] - (2 * points[8]) + points[0]) / (Globals.cellSize * Globals.cellSize);
                            double fyy = (points[2] - (2 * points[8]) + points[6]) / (Globals.cellSize * Globals.cellSize);
                            double fxy = (points[3] - points[1] - points[5] + points[7]) / (4 * Globals.cellSize * Globals.cellSize); //f3-f1-f9+f7

                            int numOfSuccess = 0;
                            List<double> successValues = new List<double>(); //keep track of each value of success
                            for (int m = -1; m < 2; m++)
                            {
                                for (int n = -1; n < 2; n++)
                                {
                                    if (m == 0 && n == 0) //center point; therefore ignore
                                    {
                                    }
                                    else if (m + i < 0 || m + i >= lengthX || n + j < 0 || n + j >= lengthY) //make sure we don't look outside of range
                                    {
                                    }
                                    else
                                    {
                                        if (gridedDataDerivIterate.Flag[i + m, j + n] != -1) //There needs to be data available within the grid
                                        {
                                            successValues.Add(gridedDataDerivIterate.Value[i + m, j + n] - (m * fx) - (n * fy) - 0.5 * ((fxx * Math.Pow(m, 2)) + (2 * m * n * fxy) + (fyy * Math.Pow(n, 2))));
                                            numOfSuccess++;
                                        }
                                    }
                                }
                            }

                            if (numOfSuccess > 0)
                            {
                                if (numOfSuccess == 2) //then we need to just do an average
                                {
                                    TaylorLoopVals[i, j] = (successValues[0] + successValues[1]) / 2; //mean
                                    gridedDataDerivIterate.Flag[i, j] = 0;
                                }
                                else if (numOfSuccess == 1)  //just directly assign
                                {
                                    TaylorLoopVals[i, j] = successValues[0];
                                    gridedDataDerivIterate.Flag[i, j] = 0;
                                }
                                else //alpha trimmed mean
                                {
                                    double tempQuart = Convert.ToDouble(numOfSuccess) / 4;
                                    int quarterNum = (int)Math.Ceiling(tempQuart);
                                    int maxQuarter = successValues.Count() - quarterNum;
                                    int tempNum = 0;
                                    double tempSum = 0;
                                    successValues.Sort(); //Sort according to the value
                                    for (int p = quarterNum; p < maxQuarter; p++)
                                    {
                                        tempSum += successValues[p];
                                        tempNum++;
                                    }
                                    TaylorLoopVals[i, j] = tempSum / tempNum; //alpha trimmed mean
                                    gridedDataDerivIterate.Flag[i, j] = 0;
                                }
                            }
                            else //this shouldn't ever happen, but if it does, then this cell is not useable
                            {
                                TaylorLoopVals[i, j] = -999999;
                                gridedDataDerivIterate.Flag[i, j] = -1;
                            }
                        }
                    }
                }

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        double tempVal = TaylorLoopVals[i, j];
                        gridedDataDerivIterate.Value[i, j] = tempVal; //replace the previous iteration with the new values
                    }
                }
                //***********************************

                //**************ANISOTROPY CALCULATOR
                //int[,,] smallestL = new int[lengthX,lengthY,4];

                //Make sure no data that is outside of the grid is being referenced
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            smallestL[i, j, k] = -1;
                            smallestLV[i, j, k] = -1;
                        }
                    }
                }

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedDataDerivIterate.Flag[i, j] == -1) //If the data is outside of the grid, then don't use it
                        {
                        }
                        else
                        {
                            double GX = 0;
                            double GY = 0;
                            double XYP = 0;
                            double XYN = 0;
                            double cells = 0;

                            double[] points = new double[9]; // 8 points around center point
                            points[0] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 0, 0], gridedData.close1[i, j, 0, 1]];
                            points[1] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 1, 0], gridedData.close1[i, j, 1, 1]];
                            points[2] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 2, 0], gridedData.close1[i, j, 2, 1]];
                            points[3] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 3, 0], gridedData.close1[i, j, 3, 1]];
                            points[4] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 4, 0], gridedData.close1[i, j, 4, 1]];
                            points[5] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 5, 0], gridedData.close1[i, j, 5, 1]];
                            points[6] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 6, 0], gridedData.close1[i, j, 6, 1]];
                            points[7] = gridedDataDerivIterate.Value[gridedData.close1[i, j, 7, 0], gridedData.close1[i, j, 7, 1]];
                            points[8] = gridedDataDerivIterate.Value[i, j];

                            //Make sure no data that is outside of the grid is being referenced
                            for (int k = 0; k < 9; k++)
                            {
                                if (points[k] == -999999)
                                {
                                    points[k] = gridedDataDerivIterate.Value[i, j];
                                    gridedData.close1[i, j, k, 0] = i;
                                    gridedData.close1[i, j, k, 1] = j;
                                }
                            }

                            //Technically the GY, GX, etc. should also be divided by the cellsize, however as they are only ever compared with each other, only their size relative to each other matters, and therfore makes this division redundant.
                            //gradient GY
                            //determine number of cells covering
                            if (gridedData.close1[i, j, 2, 1] == j || gridedData.close1[i, j, 6, 1] == j) //if the above or below cell are actually assigned to be the current cell (due to edges, etc.), then only one cell is being covered
                            {
                                cells = 1;
                            }
                            else
                            {
                                cells = 2;
                            }
                            GY = (points[2] - points[6]) / cells;

                            //gradient GX
                            //determine number of cells covering
                            if (gridedData.close1[i, j, 4, 0] == i || gridedData.close1[i, j, 0, 0] == i) //if the left or right cell are actually assigned to be the current cell (due to edges, etc.), then only one cell is being covered
                            {
                                cells = 1;
                            }
                            else
                            {
                                cells = 2;
                            }
                            GX = (points[4] - points[0]) / cells;

                            //gradient XYP
                            //determine number of cells covering
                            if (gridedData.close1[i, j, 3, 1] == j || gridedData.close1[i, j, 7, 1] == j || gridedData.close1[i, j, 3, 0] == i || gridedData.close1[i, j, 7, 0] == i) //if the above-right or below-left cell are actually assigned to be the current cell (due to edges, etc.), then only one cell is being covered
                            {
                                cells = Math.Sqrt(2);
                            }
                            else
                            {
                                cells = Math.Sqrt(2) * 2;
                            }
                            XYP = (points[3] - points[7]) / cells;

                            //gradient XYN
                            //determine number of cells covering
                            if (gridedData.close1[i, j, 5, 1] == j || gridedData.close1[i, j, 1, 1] == j || gridedData.close1[i, j, 5, 0] == i || gridedData.close1[i, j, 1, 0] == i) //if the above-left or below-right cell are actually assigned to be the current cell (due to edges, etc.), then only one cell is being covered
                            {
                                cells = Math.Sqrt(2);
                            }
                            else
                            {
                                cells = Math.Sqrt(2) * 2;
                            }
                            XYN = (points[5] - points[1]) / cells;

                            List<double> derivA = new List<double>();
                            derivA.Add(Math.Abs(GX));
                            derivA.Add(Math.Abs(GY));
                            derivA.Add(Math.Abs(XYP));
                            derivA.Add(Math.Abs(XYN));

                            //DX = 0
                            //DY = 1
                            //+DXY = 2
                            //-DXY = 3
                            derivA.Sort(); //Sort according to the value
                            //We want to find the lowest gradient direction (though may still have a large gradient).
                            bool gxF = false;
                            bool gyF = false;
                            bool xypF = false;
                            bool xynF = false;
                            for (int checkA = 0; checkA < 4; checkA++)
                            {
                                if (derivA[checkA] == Math.Abs(GX) && gxF == false)
                                {
                                    smallestL[i, j, checkA] = 0;
                                    smallestLV[i, j, checkA] = derivA[checkA];
                                    smallestLVOrder[i, j, 0] = derivA[checkA];
                                    gxF = true;
                                }
                                else if (derivA[checkA] == Math.Abs(GY) && gyF == false)
                                {
                                    smallestL[i, j, checkA] = 1;
                                    smallestLV[i, j, checkA] = derivA[checkA];
                                    smallestLVOrder[i, j, 1] = derivA[checkA];
                                    gyF = true;
                                }
                                else if (derivA[checkA] == Math.Abs(XYP) && xypF == false)
                                {
                                    smallestL[i, j, checkA] = 2;
                                    smallestLV[i, j, checkA] = derivA[checkA];
                                    smallestLVOrder[i, j, 2] = derivA[checkA];
                                    xypF = true;
                                }
                                else if (derivA[checkA] == Math.Abs(XYN) && xynF == false)
                                {
                                    smallestL[i, j, checkA] = 3;
                                    smallestLV[i, j, checkA] = derivA[checkA];
                                    smallestLVOrder[i, j, 3] = derivA[checkA];
                                    xynF = true;
                                }
                            }
                        }
                    }
                }

                //To ensure there is a smooth change over the grid, take the mode of each cell, based on it and the surrounding 8 cells
                int[, ,] smallestLTEMP = new int[lengthX, lengthY, 4];
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        bool dx = false;
                        bool dy = false;
                        bool xyp = false;
                        bool xyn = false;
                        for (int k = 0; k < 4; k++) //This process is done for each of the directions, most isotropic to most anisotropic
                        {
                            if (gridedDataDerivIterate.Flag[i, j] == -1) //If the data is outside of the grid, then don't use it
                            {
                            }
                            else
                            {
                                int[] points = new int[9]; // 8 points around center point
                                points[0] = smallestL[gridedData.close1[i, j, 0, 0], gridedData.close1[i, j, 0, 1], k];
                                points[1] = smallestL[gridedData.close1[i, j, 1, 0], gridedData.close1[i, j, 1, 1], k];
                                points[2] = smallestL[gridedData.close1[i, j, 2, 0], gridedData.close1[i, j, 2, 1], k];
                                points[3] = smallestL[gridedData.close1[i, j, 3, 0], gridedData.close1[i, j, 3, 1], k];
                                points[4] = smallestL[gridedData.close1[i, j, 4, 0], gridedData.close1[i, j, 4, 1], k];
                                points[5] = smallestL[gridedData.close1[i, j, 5, 0], gridedData.close1[i, j, 5, 1], k];
                                points[6] = smallestL[gridedData.close1[i, j, 6, 0], gridedData.close1[i, j, 6, 1], k];
                                points[7] = smallestL[gridedData.close1[i, j, 7, 0], gridedData.close1[i, j, 7, 1], k];
                                points[8] = smallestL[i, j, k];

                                //Make sure no data that is outside of the grid is being referenced
                                for (int l = 0; l < 9; l++)
                                {
                                    if (points[l] == -1)
                                    {
                                        points[l] = smallestL[i, j, k];
                                        smallestL[gridedData.close1[i, j, l, 0], gridedData.close1[i, j, l, 1], k] = smallestL[i, j, k];
                                    }
                                }

                                //another step is needed to ensure multiples of the same direction aren't added to the list
                                bool repeat = true;
                                while (repeat == true)
                                {
                                    int mode = points.GroupBy(v => v)
                                    .OrderByDescending(g => g.Count())
                                    .First()
                                    .Key;

                                    if (mode == 0 && dx == false)
                                    {
                                        dx = true;
                                        smallestLTEMP[i, j, k] = mode;
                                        repeat = false;
                                    }
                                    else if (mode == 1 && dy == false)
                                    {
                                        dy = true;
                                        smallestLTEMP[i, j, k] = mode;
                                        repeat = false;
                                    }
                                    else if (mode == 2 && xyp == false)
                                    {
                                        xyp = true;
                                        smallestLTEMP[i, j, k] = mode;
                                        repeat = false;
                                    }
                                    else if (mode == 3 && xyn == false)
                                    {
                                        xyn = true;
                                        smallestLTEMP[i, j, k] = mode;
                                        repeat = false;
                                    }
                                    else //the mode for this k value has already been recorded as a more isotropic direction. Therefore we must remove all of these values and run the mode again.
                                    {
                                        int count = 0;
                                        for (int n = 0; n < points.Length; n++) //find the number of times the mode values occurs
                                        {
                                            if (points[n] == mode)
                                            {
                                                count++;
                                            }
                                        }
                                        if (points.Length - count == 0) //Then the unfortunate circumstance occured where a value has "slipped through the cracks", and we need to manually assign it
                                        {
                                            List<int> leftover = new List<int>();
                                            leftover.Add(0);
                                            leftover.Add(1);
                                            leftover.Add(2);
                                            leftover.Add(3);
                                            for (int n = 0; n < k; n++)
                                            {
                                                leftover.Remove(smallestLTEMP[i, j, n]);
                                            }
                                            if (leftover.Count() > 1) //In this case we will just manually assign the most isotropic direction
                                            {
                                                if (leftover.Contains(smallestL[i, j, k])) //first check to see if we can just assing the previous value
                                                {
                                                    int[] tempPoints = new int[1];
                                                    tempPoints[0] = smallestL[i, j, k];
                                                    points = tempPoints;
                                                }
                                                else //if not, we must find which direction is more isotropic
                                                {
                                                    List<double> leftoverV = new List<double>();
                                                    for (int n = 0; n < leftover.Count(); n++)
                                                    {
                                                        leftoverV.Add(smallestLVOrder[i, j, leftover[n]]);
                                                    }
                                                    leftoverV.Sort();
                                                    for (int n = 0; n < leftover.Count(); n++)
                                                    {
                                                        if (leftoverV[0] == smallestLVOrder[i, j, leftover[n]])
                                                        {
                                                            int[] tempPoints = new int[1];
                                                            tempPoints[0] = leftover[n];
                                                            points = tempPoints;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                int[] tempPoints = new int[1];
                                                tempPoints[0] = leftover[0];
                                                points = tempPoints;
                                            }
                                        }
                                        else
                                        {
                                            int[] tempPoints = new int[points.Length - count];
                                            int m = 0;
                                            for (int n = 0; n < points.Length; n++) //create a new array excluding the previous mode values
                                            {
                                                if (points[n] != mode)
                                                {
                                                    tempPoints[m] = points[n];
                                                    m++;
                                                }
                                            }
                                            points = tempPoints;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            int tempSmall = smallestLTEMP[i, j, k];
                            smallestL[i, j, k] = tempSmall;
                        }
                    }
                }
                //***********************************

                //REAL DATA SCALING******************
                double[,] multiplierCells = new double[lengthX, lengthY];

                //find the real data, and the multiplier for each real data cell
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedData.Flag[i, j] == 1)
                        {
                            double tempMult = Math.Abs(gridedData.Value[i, j] / gridedDataDerivIterate.Value[i, j]);
                            multiplierCells[i, j] = tempMult;
                        }
                    }
                }

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        double tempXR = gridedDataDerivIterate.X[i, j];
                        realReplace.X[i, j] = tempXR;
                        double tempYR = gridedDataDerivIterate.Y[i, j];
                        realReplace.Y[i, j] = tempYR;

                        if (gridedData.Flag[i, j] == 1)
                        {
                            double tempVR = gridedData.Value[i, j];
                            realReplace.Value[i, j] = tempVR;
                            realReplace.Flag[i, j] = 1; //real
                        }
                        else if (gridedDataDerivIterate.Flag[i, j] == -1)
                        {
                            realReplace.Value[i, j] = -999999;
                            realReplace.Flag[i, j] = -1; //out of range
                        }
                        else //otherwise it is interpolated data, and therefore we must find the real data along lines of isotropy
                        {
                            bool failed = false;
                            bool multFind = false;
                            double iMult = 0;
                            int multI = 0;
                            int multJ = 0;
                            int mostIso = 0;
                            List<int> key = new List<int>(); //a list to keep track of which directions have been searched:
                            //0 = dy-xyp
                            //1 = dx-xyp
                            //2 = dx-xyn
                            //3 = dy-xyn
                            int[,] key2 = new int[4, 2]; //another key to keep track of which two directions are possible for current direction
                            key2[0, 0] = 1; //dx - dx-xyp
                            key2[0, 1] = 2; //dx - dx-xyn
                            key2[1, 0] = 0; //dy - dy-xyp
                            key2[1, 1] = 3; //dy - dy-xyn
                            key2[2, 0] = 0; //xyp - xyp-dy
                            key2[2, 1] = 1; //xyp - xyp-dx
                            key2[3, 0] = 2; //xyn - xyn-dx
                            key2[3, 1] = 3; //xyn - xyn-dy

                            int[,] key3 = new int[4, 2]; //another key to keep track of which other directions are possible for current direction (reverse of key2)
                            key3[0, 0] = 1; //dy-xyp - dy
                            key3[0, 1] = 2; //dy-xyp - xyp
                            key3[1, 0] = 0; //dx-xyp - dx
                            key3[1, 1] = 2; //dx-xyp - xyp
                            key3[2, 0] = 0; //dx-xyn - dx
                            key3[2, 1] = 3; //dx-xyn - xyn
                            key3[3, 0] = 1; //dy-xyn - dy
                            key3[3, 1] = 3; //dy-xyn - xyn

                            double totalLV = 0; //the total value of the gradients from the trend analysis. We will use this to analyze the strength of the gradients.
                            for (int k = 0; k < 4; k++)
                            {
                                totalLV = totalLV + smallestLV[i, j, k];
                            }

                            while (multFind == false)
                            {
                                if (key.Contains(key2[smallestL[i, j, mostIso], 0]) && key.Contains(key2[smallestL[i, j, mostIso], 1])) //if we've already completed both directions
                                {
                                    mostIso++;
                                    if (mostIso >= 3)
                                    {
                                        failed = true; //failed for some reason
                                        multFind = true;
                                    }
                                }
                                else //we haven't completed at least one of the directions
                                {
                                    int checkDir = -1;
                                    int dir1 = -1;
                                    int dir2 = -1;
                                    //find which one (or two) we have completed
                                    if (key.Contains(key2[smallestL[i, j, mostIso], 0]))
                                    {
                                        checkDir = key2[smallestL[i, j, mostIso], 1]; //set the checkDir to the other
                                        key.Add(checkDir); //add the direction we are now searching to the key to let us know for later that we have tried it
                                        if (smallestL[i, j, mostIso] == key3[checkDir, 0])
                                        {
                                            dir1 = key3[checkDir, 1];
                                            dir2 = -1;
                                        }
                                        else
                                        {
                                            dir1 = key3[checkDir, 0];
                                            dir2 = -1;
                                        }
                                    }
                                    else if (key.Contains(key2[smallestL[i, j, mostIso], 1]))
                                    {
                                        checkDir = key2[smallestL[i, j, mostIso], 0]; //set the checkDir to the other
                                        key.Add(checkDir); //add the direction we are now searching to the key to let us know for later that we have tried it
                                        if (smallestL[i, j, mostIso] == key3[checkDir, 0])
                                        {
                                            dir1 = key3[checkDir, 1];
                                            dir2 = -1;
                                        }
                                        else
                                        {
                                            dir1 = key3[checkDir, 0];
                                            dir2 = -1;
                                        }
                                    }
                                    else //neither direction has been searched yet, so we need to find which is higher up on the list from trend analysis
                                    {
                                        int checkDir1 = key2[smallestL[i, j, mostIso], 0];
                                        int checkDir2 = key2[smallestL[i, j, mostIso], 1];
                                        if (smallestL[i, j, mostIso] == key3[checkDir1, 0])
                                        {
                                            dir1 = key3[checkDir1, 1];
                                        }
                                        else
                                        {
                                            dir1 = key3[checkDir1, 0];
                                        }
                                        if (smallestL[i, j, mostIso] == key3[checkDir2, 0])
                                        {
                                            dir2 = key3[checkDir2, 1];
                                        }
                                        else
                                        {
                                            dir2 = key3[checkDir2, 0];
                                        }
                                    }

                                    //investigate the one or two directions we need to check
                                    int firstInLine = -1;
                                    int track = 0;
                                    while (firstInLine == -1) //check which one is higher up in the list
                                    {
                                        if (smallestL[i, j, track] == dir1)
                                        {
                                            firstInLine = dir1;
                                        }
                                        else if (smallestL[i, j, track] == dir2)
                                        {
                                            firstInLine = dir2;
                                        }
                                        else
                                        {
                                            track++;
                                        }
                                    }
                                    if (dir2 != -1) //neither direction had been searched, and therefore the direction hadn't been determined and added to the key yet
                                    {
                                        if (firstInLine == 0 || firstInLine == 1) //if dx or dy, look at 2 and 3
                                        {
                                            if (smallestL[i, j, mostIso] == 2) //XYP
                                            {
                                                multI = 1;
                                                multJ = 1;
                                                if (firstInLine == 0) //dx-xyp
                                                {
                                                    key.Add(1);
                                                }
                                                else //dy-xyp
                                                {
                                                    key.Add(0);
                                                }
                                            }
                                            else //XYN
                                            {
                                                multI = 1;
                                                multJ = -1;
                                                if (firstInLine == 0) //dx-xyn
                                                {
                                                    key.Add(2);
                                                }
                                                else //dy-xyn
                                                {
                                                    key.Add(3);
                                                }
                                            }
                                        }
                                        else //else it is +dxy or -dxy, in which case look at 0 and 1
                                        {
                                            if (smallestL[i, j, mostIso] == 0) //dx
                                            {
                                                if (firstInLine == 2) //dx-xyp
                                                {
                                                    multI = 1;
                                                    multJ = 1;
                                                    key.Add(1);
                                                }
                                                else //dx-xyn
                                                {
                                                    multI = 1;
                                                    multJ = -1;
                                                    key.Add(2);
                                                }
                                            }
                                            else //dy
                                            {
                                                if (firstInLine == 2) //dy-xyp
                                                {
                                                    multI = 1;
                                                    multJ = 1;
                                                    key.Add(0);
                                                }
                                                else //dy-xyn
                                                {
                                                    multI = 1;
                                                    multJ = -1;
                                                    key.Add(3);
                                                }
                                            }
                                        }
                                    }
                                    else //key already added, we just need the multI multJ determined
                                    {
                                        if (smallestL[i, j, mostIso] == 2 || dir1 == 2) //xyp
                                        {
                                            multI = 1;
                                            multJ = 1;
                                        }
                                        else //xyn
                                        {
                                            multI = 1;
                                            multJ = -1;
                                        }
                                    }

                                    double XoY = -1; //need to know if we are measuring the angle from the x-axis (0) or the y-axis (1)
                                    double sideDist1 = -1;
                                    double sideDist2 = -1;
                                    //Now we need to find the distance weighted average between these two sides, and use this to find the angle we wish to look.
                                    //We have to measure from the x or y axis for the angle to work.
                                    if (smallestL[i, j, mostIso] == 0 || smallestL[i, j, mostIso] == 1) //if dx or dy, then use it
                                    {
                                        sideDist1 = smallestLV[i, j, mostIso];
                                        sideDist2 = smallestLV[i, j, track];
                                        if (smallestL[i, j, mostIso] == 0)
                                        {
                                            XoY = 0; //we are measuring from the x-axis
                                        }
                                        else
                                        {
                                            XoY = 1; //we are measuring from the y-axis
                                        }
                                    }
                                    else //else it is +dxy or -dxy, in which case use the m location (which will be x or y axis)
                                    {
                                        sideDist1 = smallestLV[i, j, track];
                                        sideDist2 = smallestLV[i, j, mostIso];
                                        if (smallestL[i, j, track] == 0)
                                        {
                                            XoY = 0; //we are measuring from the x-axis
                                        }
                                        else
                                        {
                                            XoY = 1; //we are measuring from the y-axis
                                        }
                                    }

                                    double totSideDist = sideDist1 + sideDist2;
                                    double distToAngle = 0;
                                    if (totSideDist == 0) //then both are zero, in which case both sides are perfectly isotropic. Therefore let's just look directly between them both (i.e. 22.5 degrees).
                                    {
                                        distToAngle = Math.Tan((Math.PI * (45 / 2) / 180));
                                    }
                                    else
                                    {
                                        distToAngle = sideDist1 / totSideDist;
                                    }

                                    //Therefore the angle is going to be:
                                    double angleMult = distToAngle * ((Math.PI * 45) / 180);

                                    double smallSide = 0;
                                    if (sideDist1 < sideDist2)
                                    {
                                        smallSide = sideDist1;
                                    }
                                    else
                                    {
                                        smallSide = sideDist2;
                                    }

                                    strength[i, j] = smallSide / totalLV; //find this "strength" identifier, which will generally range between 0 and 0.25, though in more extreme gradient cases it could range higher (techincally up to 0.9)
                                    //we will now force it to range only between 0 and 0.25, as this helps ensure a better range of values for later use.
                                    /*if (strength[i, j] < 0)
                                    {
                                        strength[i, j] = 0;
                                    }
                                    else if (strength[i, j] > 0.25)
                                    {
                                        strength[i, j] = 0.25;
                                    }*/

                                    //Now with the angle found, we need to look in both of these directions, and find the locations of the first real data cells we run into.
                                    double searchStep = 0.5 / Globals.searchStepSize; //start at the point where it would be in the next cell if looking directly along axes
                                    bool searching = true;
                                    bool searchPos = false;
                                    bool posGood = false;
                                    bool searchNeg = false;
                                    bool negGood = false;
                                    int iT1 = 0;
                                    int jT1 = 0;
                                    int iT1_2 = 0;
                                    int jT1_2 = 0;
                                    int iT1_3 = 0;
                                    int jT1_3 = 0;
                                    int iT2 = 0;
                                    int jT2 = 0;
                                    int iT2_2 = 0;
                                    int jT2_2 = 0;
                                    int iT2_3 = 0;
                                    int jT2_3 = 0;
                                    bool foundNearbyCellPos = false;
                                    bool foundNearbyCellPos2 = false;
                                    bool foundNearbyCellNeg = false;
                                    bool foundNearbyCellNeg2 = false;
                                    while (searching == true)
                                    {
                                        //find the distance we are looking
                                        double tempXD = 0;
                                        double tempYD = 0;
                                        double currentStep = Globals.searchStepSize * searchStep;
                                        if (XoY == 0)
                                        {
                                            tempXD = currentStep * Math.Cos(angleMult);
                                            tempYD = currentStep * Math.Sin(angleMult);
                                        }
                                        else
                                        {
                                            tempYD = currentStep * Math.Cos(angleMult);
                                            tempXD = currentStep * Math.Sin(angleMult);
                                        }
                                        //now we need to know which direction we are looking, and add the current distance to i and j (current location)
                                        if (searchPos == false) //we have not found a real data cell in the positive direction yet
                                        {
                                            int tempI = Convert.ToInt32(multI * Math.Floor(tempXD + 0.5));
                                            int tempJ = Convert.ToInt32(multJ * Math.Floor(tempYD + 0.5));
                                            if (i + tempI >= lengthX || i + tempI < 0) //hit an edge in x-direction
                                            {
                                                searchPos = true;
                                                posGood = false;
                                            }
                                            else if (j + tempJ >= lengthY || j + tempJ < 0) //hit an edge y-direction
                                            {
                                                searchPos = true;
                                                posGood = false;
                                            }
                                            else if (gridedDataDerivIterate.Flag[i + tempI, j + tempJ] == -1) //outside of interpolation distance
                                            {
                                                searchPos = true;
                                                posGood = false;
                                            }
                                            else if (gridedData.Flag[i + tempI, j + tempJ] == 1) //if true, then we have found a real data cell
                                            {
                                                //find out which side we came in from
                                                double XPM = gridedData.X[i + tempI, j + tempJ] - (gridedData.X[i, j] + (multI * (tempXD + 0.5)));
                                                double YPM = gridedData.Y[i + tempI, j + tempJ] - (gridedData.Y[i, j] + (multJ * (tempYD + 0.5)));

                                                for (int k = 0; k < 8; k++)
                                                {
                                                    if (gridedData.Flag[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] == 1) //this cell is real
                                                    {
                                                        int wayX = Convert.ToInt32((gridedData.X[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] - gridedData.X[i + tempI, j + tempJ]) / Globals.cellSize);
                                                        int wayY = Convert.ToInt32((gridedData.Y[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] - gridedData.Y[i + tempI, j + tempJ]) / Globals.cellSize);
                                                        if (Math.Sign(wayX) == Math.Sign(XPM) || wayX == 0)
                                                        {
                                                            if (Math.Sign(wayY) == Math.Sign(YPM) || wayY == 0)
                                                            {
                                                                //in the rare case where two are found, we need to average over all
                                                                if (foundNearbyCellPos == true)
                                                                {
                                                                    iT1_3 = tempI + wayX;
                                                                    jT1_3 = tempJ + wayY;
                                                                    foundNearbyCellPos2 = true;
                                                                }
                                                                else
                                                                {
                                                                    iT1_2 = tempI + wayX;
                                                                    jT1_2 = tempJ + wayY;
                                                                    foundNearbyCellPos = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                iT1 = tempI;
                                                jT1 = tempJ;
                                                searchPos = true;
                                                posGood = true;
                                            }
                                        }
                                        if (searchNeg == false) //we have not found a real data cell in the negative direction yet
                                        {
                                            int tempI = Convert.ToInt32(-multI * Math.Floor(tempXD + 0.5));
                                            int tempJ = Convert.ToInt32(-multJ * Math.Floor(tempYD + 0.5));
                                            if (i + tempI >= lengthX || i + tempI < 0) //hit an edge in x-direction
                                            {
                                                searchNeg = true;
                                                negGood = false;
                                            }
                                            else if (j + tempJ >= lengthY || j + tempJ < 0) //hit an edge y-direction
                                            {
                                                searchNeg = true;
                                                negGood = false;
                                            }
                                            else if (gridedDataDerivIterate.Flag[i + tempI, j + tempJ] == -1) //outside of interpolation distance
                                            {
                                                searchNeg = true;
                                                negGood = false;
                                            }
                                            else if (gridedData.Flag[i + tempI, j + tempJ] == 1) //if true, then we have found a real data cell
                                            {
                                                //find out which side we came in from
                                                double XPM = gridedData.X[i + tempI, j + tempJ] - (gridedData.X[i, j] + (multI * (tempXD + 0.5)));
                                                double YPM = gridedData.Y[i + tempI, j + tempJ] - (gridedData.Y[i, j] + (multJ * (tempYD + 0.5)));

                                                for (int k = 0; k < 8; k++)
                                                {
                                                    if (gridedData.Flag[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] == 1) //this cell is real
                                                    {
                                                        int wayX = Convert.ToInt32((gridedData.X[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] - gridedData.X[i + tempI, j + tempJ]) / Globals.cellSize);
                                                        int wayY = Convert.ToInt32((gridedData.Y[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] - gridedData.Y[i + tempI, j + tempJ]) / Globals.cellSize);
                                                        if (Math.Sign(wayX) == Math.Sign(XPM) || wayX == 0)
                                                        {
                                                            if (Math.Sign(wayY) == Math.Sign(YPM) || wayY == 0)
                                                            {
                                                                //in the rare case where two are found, we need to average over all
                                                                if (foundNearbyCellNeg == true)
                                                                {
                                                                    iT2_3 = tempI + wayX;
                                                                    jT2_3 = tempJ + wayY;
                                                                    foundNearbyCellNeg2 = true;
                                                                }
                                                                else
                                                                {
                                                                    iT2_2 = tempI + wayX;
                                                                    jT2_2 = tempJ + wayY;
                                                                    foundNearbyCellNeg = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                iT2 = tempI;
                                                jT2 = tempJ;
                                                searchNeg = true;
                                                negGood = true;
                                            }
                                        }
                                        if (searchPos == false || searchNeg == false) //have not yet found a real data cell in one or both directions yet
                                        {
                                            searchStep++;
                                            if (Globals.searchStepSize * searchStep >= (1.5 * Globals.interpDist) / Globals.cellSize) //then we have not found any real data cell within the user-defined interpolation distance
                                            {
                                                searching = false; //exit the loop
                                            }
                                        }
                                        else //both are true, and therefore we are done searching
                                        {
                                            searching = false;
                                        }
                                    }

                                    if (posGood == true && negGood == true) //then both directions succeeeded
                                    {
                                        double multiplierCPos = 0;
                                        double multiplierCNeg = 0;
                                        if (foundNearbyCellPos == true)
                                        {
                                            multiplierCPos = (multiplierCells[i + iT1, j + jT1] + multiplierCells[i + iT1_2, j + jT1_2]) / 2;
                                        }
                                        else if (foundNearbyCellPos2 == true)
                                        {
                                            multiplierCPos = (multiplierCells[i + iT1, j + jT1] + multiplierCells[i + iT1_2, j + jT1_2] + multiplierCells[i + iT1_3, j + jT1_3]) / 3;
                                        }
                                        else
                                        {
                                            multiplierCPos = multiplierCells[i + iT1, j + jT1];
                                        }
                                        if (foundNearbyCellNeg == true)
                                        {
                                            multiplierCNeg = (multiplierCells[i + iT2, j + jT2] + multiplierCells[i + iT2_2, j + jT2_2]) / 2;
                                        }
                                        else if (foundNearbyCellNeg2 == true)
                                        {
                                            multiplierCNeg = (multiplierCells[i + iT2, j + jT2] + multiplierCells[i + iT2_2, j + jT1_2] + multiplierCells[i + iT2_3, j + jT2_3]) / 3;
                                        }
                                        else
                                        {
                                            multiplierCNeg = multiplierCells[i + iT2, j + jT2];
                                        }
                                        double distance1 = Math.Sqrt(Math.Pow(iT1, 2) + Math.Pow(jT1, 2));
                                        double distance2 = Math.Sqrt(Math.Pow(iT2, 2) + Math.Pow(jT2, 2));
                                        double totDist = distance1 + distance2;
                                        double iMultT1 = distance2 * (multiplierCPos) / totDist;
                                        double iMultT2 = distance1 * (multiplierCNeg) / totDist;
                                        iMult = iMultT1 + iMultT2;
                                        multFind = true;
                                    }
                                    else if (posGood == true)
                                    {
                                        if (foundNearbyCellPos == true)
                                        {
                                            iMult = (multiplierCells[i + iT1, j + jT1] + multiplierCells[i + iT1_2, j + jT1_2]) / 2;
                                        }
                                        else
                                        {
                                            iMult = multiplierCells[i + iT1, j + jT1];
                                        }
                                        multFind = true;
                                    }
                                    else if (negGood == true)
                                    {
                                        if (foundNearbyCellNeg == true)
                                        {
                                            iMult = (multiplierCells[i + iT2, j + jT2] + multiplierCells[i + iT2_2, j + jT2_2]) / 2;
                                        }
                                        else
                                        {
                                            iMult = multiplierCells[i + iT2, j + jT2];
                                        }
                                        multFind = true;
                                    }
                                    else //both directions failed. We must now start over with a new direction.
                                    {
                                    }
                                }
                            }
                            if (failed == true)
                            {
                                realReplace.Value[i, j] = -999999; //failed data
                                realReplace.Flag[i, j] = -1; //outside of grid
                            }
                            else
                            {
                                //iMult = 1 - ((1 - iMult) * (1 - strength[i, j]));
                                double tempMR = gridedDataDerivIterate.Value[i, j] * iMult;
                                if (tempMR > maxVal) //do not let the scaled data become greater than the max value found in the raw data
                                {
                                    tempMR = maxVal;
                                }
                                realReplace.Value[i, j] = tempMR; //replace with scaled data
                                realReplace.Flag[i, j] = 0; //not real
                            }
                        }
                    }
                }
                //***********************************

                //Stopping criteria check************
                if (currentLoop != 0)
                {
                    List<double> diffs = new List<double>();

                    for (int i = 0; i < lengthX; i++)
                    {
                        for (int j = 0; j < lengthY; j++)
                        {
                            if (realReplace.Flag[i, j] != -1)
                            {
                                double tempVal = Math.Abs(realReplace.Value[i, j] - previousGrid.Value[i, j]);
                                diffs.Add(tempVal);
                            }
                        }
                    }

                    double diffAvg = diffs.Average();
                    double sum = diffs.Sum(d => Math.Pow(d - diffAvg, 2));
                    double diffStd = Math.Sqrt((sum) / (diffs.Count() - 1));

                    diffs.Sort();
                    double diffMedi = diffs[diffs.Count / 2];

                    if (currentLoop != 1)
                    {
                        double meanDiff = diffAvg - previousMean;
                        double medianDiff = diffMedi - previousMedian;
                        if (meanDiff >= 0 && medianDiff >= 0)
                        {
                            numStops++;
                        }
                        else
                        {
                            previousMean = diffAvg;
                            previousMedian = diffMedi;
                        }
                    }
                    else
                    {
                        previousMean = diffAvg;
                        previousMedian = diffMedi;
                    }

                }
                //***********************************
                currentLoop++;
                if (numStops >= 3)
                {
                    looping = 1;
                }
            }
            currentLoop = currentLoop - 1;
            //***********************************

            //*******************SUBTRACT MIN VAL
            if (minVal < 0)
            {
                //Subtract off the min val found earlier
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (realReplace.Flag[i, j] != -1)
                        {
                            realReplace.Value[i, j] = realReplace.Value[i, j] - Math.Abs(minVal);
                        }
                    }
                }
            }
            //***********************************

            //*****************************OUTPUT
            System.IO.StreamWriter myfile2 = new System.IO.StreamWriter(Globals.outputFile, true);
            //file. setprecision(10); //Set the precision of the output data to 10 sig digs. Do this to not allow incorrect values in sci notation.
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    myfile2.WriteLine(realReplace.X[i, j] + "\t" + realReplace.Y[i, j] + "\t" + realReplace.Value[i, j]);
                }
            }
            myfile2.Close();
            //***********************************

        }
    }
}
