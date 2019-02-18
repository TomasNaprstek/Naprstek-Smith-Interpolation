using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using GeoEngine.Core.GXNet;
using Geosoft.GX.Controls;
using Geosoft.Desktop.GXNet;
using CoreConstant = GeoEngine.Core.GXNet.Constant;

namespace NaprstekSmithV15
{
    static class Globals
    {
        //*USER-INPUT VARIABLES
        public static string inputFile = "Database"; //input database
        public static string xName = "x"; //the name of the x channel
        public static string yName = "y"; //the name of the y channel
        public static string dataName = "data"; //the name of the data channel
        public static string outputFile = ""; //output file
        public static double cellSize = 50; //edge size of each square cell in metres
        public static double interpDist = 250; //metres away that will be interpolated
        public static int maxLoop = 50; //the number of times the interpolation loop will be processed
        public static double searchStepSize = 0.25; //how much of a cell we will "travel" each search step
        public static double cellSizeF = 100; //resampled final cell size
        public static double trendM = 100; //100 - median % location (so 0 is no trending, 100 is full trending)
        public static bool autoStop = true; //a checkbox of whether or not to auto stop
        public static double angleSearch = 10; //the number of degrees it will move each time when searching away from the initial eigenvector
        //*********************
    }

    class cellData
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

    public class Execute : BaseForm
    {
        private System.Windows.Forms.Label m_lblX;
        private System.Windows.Forms.TextBox m_tbX;

        //Constructor
        public Execute()
            : base()
        {
            InitializeForm();
        }

        public Execute(IntPtr pGeo)
            : base(pGeo)
        {
            InitializeForm();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region BaseForm Overrides
        protected override void InitializeForm()
        {

            StartGrowFormDuringTranslation();
            TranslateControls();
            base.InitializeForm();



            EndGrowFormDuringTranslation();
        }


        public override bool bValidateDialog(ref string strINIError, bool bUseErrorProvider)
        {
            bool bValid = true;

            if (!base.bValidateDialog(ref strINIError, bUseErrorProvider))
                bValid = false;

            if (bValid)
            {

            }
            return bValid;
        }

        #endregion

        private static DialogResult ShowInputDialog()
        {
            System.Drawing.Size size = new System.Drawing.Size(340, 380);
            Form inputBox = new Form();

            inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            inputBox.ClientSize = size;
            inputBox.Text = "User Variables";
            inputBox.MinimizeBox = false;
            inputBox.MaximizeBox = false;

            //Database
            Label textLabel = new Label() { Left = 5, Top = 5, Text = "Enter database name" };
            textLabel.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel);

            System.Windows.Forms.TextBox textBox = new TextBox();
            textBox.Size = new System.Drawing.Size(160, 23);
            textBox.Location = new System.Drawing.Point(5, 28);
            textBox.Text = Globals.inputFile;
            inputBox.Controls.Add(textBox);

            //Data channel
            Label textLabel1 = new Label() { Left = 175, Top = 5, Text = "Enter data channel name" };
            textLabel1.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel1);

            System.Windows.Forms.TextBox textBox1 = new TextBox();
            textBox1.Size = new System.Drawing.Size(160, 23);
            textBox1.Location = new System.Drawing.Point(175, 28);
            textBox1.Text = Globals.dataName;
            inputBox.Controls.Add(textBox1);

            //x channel
            Label textLabel2 = new Label() { Left = 5, Top = 60, Text = "Enter x position channel name" };
            textLabel2.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel2);

            System.Windows.Forms.TextBox textBox2 = new TextBox();
            textBox2.Size = new System.Drawing.Size(160, 23);
            textBox2.Location = new System.Drawing.Point(5, 83);
            textBox2.Text = Globals.xName;
            inputBox.Controls.Add(textBox2);

            //y channel
            Label textLabel3 = new Label() { Left = 175, Top = 60, Text = "Enter y position channel name" };
            textLabel3.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel3);

            System.Windows.Forms.TextBox textBox3 = new TextBox();
            textBox3.Size = new System.Drawing.Size(160, 23);
            textBox3.Location = new System.Drawing.Point(175, 83);
            textBox3.Text = Globals.yName;
            inputBox.Controls.Add(textBox3);

            //GRID CELL SIZE
            Label textLabel4 = new Label() { Left = 5, Top = 115, Text = "Enter grid cell size" };
            textLabel4.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel4);

            System.Windows.Forms.TextBox textBox4 = new TextBox();
            textBox4.Size = new System.Drawing.Size(160, 23);
            textBox4.Location = new System.Drawing.Point(5, 138);
            textBox4.Text = Convert.ToString(Globals.cellSize);
            inputBox.Controls.Add(textBox4);

            //INTERPOLATION DISTANCE
            Label textLabel6 = new Label() { Left = 175, Top = 115, Text = "Enter interpolation distance" };
            textLabel6.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel6);

            System.Windows.Forms.TextBox textBox6 = new TextBox();
            textBox6.Size = new System.Drawing.Size(160, 23);
            textBox6.Location = new System.Drawing.Point(175, 138);
            textBox6.Text = Convert.ToString(Globals.interpDist);
            inputBox.Controls.Add(textBox6);

            //FINAL CELL SIZE
            Label textLabel7 = new Label() { Left = 5, Top = 170, Text = "Enter Final Cell Size" };
            textLabel7.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel7);

            System.Windows.Forms.TextBox textBox7 = new TextBox();
            textBox7.Size = new System.Drawing.Size(160, 23);
            textBox7.Location = new System.Drawing.Point(5, 193);
            textBox7.Text = Convert.ToString(Globals.cellSizeF);
            inputBox.Controls.Add(textBox7);

            //MAX NUMBER OF LOOPS
            Label textLabel8 = new Label() { Left = 175, Top = 170, Text = "Enter maximum # of loops" };
            textLabel8.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel8);

            System.Windows.Forms.TextBox textBox8 = new TextBox();
            textBox8.Size = new System.Drawing.Size(160, 23);
            textBox8.Location = new System.Drawing.Point(175, 193);
            textBox8.Text = Convert.ToString(Globals.maxLoop);
            inputBox.Controls.Add(textBox8);

            //TREND MULTIPLIER
            Label textLabel9 = new Label() { Left = 5, Top = 225, Text = "Trending Factor (0-100)" };
            textLabel9.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel9);

            System.Windows.Forms.TextBox textBox9 = new TextBox();
            textBox9.Size = new System.Drawing.Size(160, 23);
            textBox9.Location = new System.Drawing.Point(5, 248);
            textBox9.Text = Convert.ToString(Globals.trendM);
            inputBox.Controls.Add(textBox9);

            //ANGLE SEARCH
            Label textLabel11 = new Label() { Left = 175, Top = 225, Text = "Theta" };
            textLabel9.Size = new System.Drawing.Size(160, 23);
            inputBox.Controls.Add(textLabel11);

            System.Windows.Forms.TextBox textBox10 = new TextBox();
            textBox10.Size = new System.Drawing.Size(160, 23);
            textBox10.Location = new System.Drawing.Point(175, 248);
            textBox10.Text = Convert.ToString(Globals.angleSearch);
            inputBox.Controls.Add(textBox10);

            //AUTOSTOP CHECKBOX
            Label textLabel10 = new Label() { Left = 25, Top = 280, Text = "Use Automatic Stopping Criteria?" };
            textLabel10.Size = new System.Drawing.Size(size.Width - 30, 23);
            inputBox.Controls.Add(textLabel10);

            System.Windows.Forms.CheckBox chkbxAuto = new CheckBox();
            chkbxAuto.Location = new System.Drawing.Point(5, 280);
            chkbxAuto.Checked = Globals.autoStop;
            inputBox.Controls.Add(chkbxAuto);

            //BUTTONS
            Button okButton = new Button();
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.Text = "&OK";
            okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 350);
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new System.Drawing.Point(size.Width - 80, 350);
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            Globals.inputFile = textBox.Text;
            Globals.dataName = textBox1.Text;
            Globals.xName = textBox2.Text;
            Globals.yName = textBox3.Text;
            Globals.cellSize = Convert.ToDouble(textBox4.Text);
            Globals.interpDist = Convert.ToDouble(textBox6.Text);
            Globals.cellSizeF = Convert.ToDouble(textBox7.Text);
            Globals.maxLoop = Convert.ToInt32(textBox8.Text);
            Globals.trendM = Convert.ToDouble(textBox9.Text);
            Globals.angleSearch = Convert.ToDouble(textBox10.Text);
            Globals.autoStop = chkbxAuto.Checked;
            return result;
        }

        [CGXAttribute("")]
        public static void Main()
        {
            //***********USER INPUT
            //string input = Globals.inputFile;
            DialogResult formResult = ShowInputDialog();

            if (formResult != DialogResult.OK)
            {
                MessageBox.Show("Process cancelled.");
                return;
            }

            //****GEOSOFT VARIABLES
            CGX_NET pGeo = null;
            CDB hDB = null;
            CVV hVVx = null;
            CVV hVVy = null;
            CVV hVVv = null;
            CVM hVMx = null;
            CVM hVMy = null;
            CVM hVMv = null;
            CVA hVA = null;

            Int32 xChan, yChan, vChan, iLine; // database symbols

            int iVA, iL, iLines, iS;
            DoubleVector dDatax = null;
            DoubleVector dDatay = null;
            DoubleVector dDatav = null;

            //****GEOSOFT IMPORTING
            // --- Initialize ---
            pGeo = new CGX_NET();
            if (pGeo == null)
            {
                MessageBox.Show("Unable to start Geosoft session");
                return;
            }

            // --- 
            // Open a database.
            // Note that all databases created by normal Geosoft users will have
            // user name "SUPER" with password "".  Third-party or custom applications
            // may create databases that require a user-name and password, in which
            // case you require these values to open the database.
            // ---
            hDB = CDB.Open(Globals.inputFile, "SUPER", "");

            // --- Get the channel handle ---
            xChan = hDB.FindSymb(Globals.xName, CoreConstant.DB_SYMB_CHAN);
            if (xChan == CoreConstant.NULLSYMB)
            {
                MessageBox.Show(Globals.xName + " channel not found in database " + Globals.inputFile);
                return;
            }
            yChan = hDB.FindSymb(Globals.yName, CoreConstant.DB_SYMB_CHAN);
            if (yChan == CoreConstant.NULLSYMB)
            {
                MessageBox.Show(Globals.yName + " channel not found in database " + Globals.inputFile);
                return;
            }
            vChan = hDB.FindSymb(Globals.dataName, CoreConstant.DB_SYMB_CHAN);
            if (vChan == CoreConstant.NULLSYMB)
            {
                MessageBox.Show(Globals.dataName + " channel not found in database " + Globals.inputFile);
                return;
            }

            // --- Lock channel for read-writer ---
            hDB.LockSymb(xChan, CoreConstant.DB_LOCK_READWRITE, CoreConstant.DB_WAIT_NONE);
            hDB.LockSymb(yChan, CoreConstant.DB_LOCK_READWRITE, CoreConstant.DB_WAIT_NONE);
            hDB.LockSymb(vChan, CoreConstant.DB_LOCK_READWRITE, CoreConstant.DB_WAIT_NONE);

            // --- do we need a VV or a VA? ---
            iVA = hDB.iGetColVA(xChan); // number of elements in a VA, 1 if its a VV
            if (iVA == 1)
            {
                // --- Create a VV to hold data array ---
                hVVx = CVV.CreateExt(CoreConstant.GS_DOUBLE, 0);
                hVVy = CVV.CreateExt(CoreConstant.GS_DOUBLE, 0);
                hVVv = CVV.CreateExt(CoreConstant.GS_DOUBLE, 0);
            }
            else
            {
                // --- its a VA channel ---
                hVA = CVA.CreateExt(CoreConstant.GS_DOUBLE, 0, iVA);
                MessageBox.Show("Need to write code here!");
            }

            // --- Get a real VM to hold the data in memory ---
            hVMx = CVM.Create(CoreConstant.GS_REAL, 0);
            hVMy = CVM.Create(CoreConstant.GS_REAL, 0);
            hVMv = CVM.Create(CoreConstant.GS_REAL, 0);

            // --- Count selected lines ---
            iLines = hDB.iCountSelLines();

            // --- Go through all selected lines ---
            iL = 0;
            iLine = hDB.FirstSelLine();

            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            List<double> Value = new List<double>();
            List<double> Line = new List<double>();

            CSYS.Progress(1);
            CSYS.ProgName("Reading database.", 0);

            do
            {
                // --- Break if line is not a valid line ---
                if (hDB.iIsLineValid(iLine) == 0) break;

                // --- Update progress ---
                CSYS.ProgUpdateL(iL, iLines);

                // --- Read Data ---
                if (iVA == 1)
                {
                    // --- get VV data ---
                    hDB.GetChanVV(iLine, xChan, hVVx);
                    hDB.GetChanVV(iLine, yChan, hVVy);
                    hDB.GetChanVV(iLine, vChan, hVVv);
                }
                else
                {
                    MessageBox.Show("Need code here");
                    // --- get VA data ---
                    //hDB.GetChanVA(iLine, iChan, hVA);

                    // --- 
                    // Get the VV to the VA data.  This VV contains all VA data
                    // by element, then by row.  The VV is owned by the VA, so you
                    // cannot destroy the VA or the VV will become invalid.
                    // ---

                    //hVV = hVA.GetFullVV();
                }

                // --- 
                // Get data VM from the VV.  This will re-size the VM to
                // hold all the data in the VV.
                // ---

                CVV.CopyVVtoVM(hVMx, hVVx);
                iS = hVVx.iLength();
                dDatax = CGX_NET.GetDoubleVM(hVMx, iS);
                for (int i = 0; i < iS; i++)
                {
                    X.Add(dDatax[i]);
                }

                CVV.CopyVVtoVM(hVMy, hVVy);
                iS = hVVy.iLength();
                dDatay = CGX_NET.GetDoubleVM(hVMy, iS);
                for (int i = 0; i < iS; i++)
                {
                    Y.Add(dDatay[i]);
                }

                CVV.CopyVVtoVM(hVMv, hVVv);
                iS = hVVv.iLength();
                dDatav = CGX_NET.GetDoubleVM(hVMv, iS);
                for (int i = 0; i < iS; i++)
                {
                    Value.Add(dDatav[i]);
                }

                // --- Advance to Next Line ---
                iLine = hDB.NextSelLine(iLine);
                iL++;
            } while (true);

            //cleanup
            CSYS.Progress(0);
            hDB.UnLockSymb(xChan);
            hDB.UnLockSymb(yChan);
            hDB.UnLockSymb(vChan);
            if (hVMx != null) hVMx.Dispose();
            if (hVMy != null) hVMy.Dispose();
            if (hVMv != null) hVMv.Dispose();
            if (hVVx != null && (iVA != 1)) hVVx.Dispose();
            if (hVVy != null && (iVA != 1)) hVVy.Dispose();
            if (hVVv != null && (iVA != 1)) hVVv.Dispose();
            hDB.Dispose();
            //***********************************

            //****************FULL GRID PROCEDURE
            CSYS.Progress(1);
            CSYS.ProgName("Gridding Pt 1", 0);
            //Find and remove any dummy values
            X.RemoveAll(i => i.Equals(-2147483647));
            Y.RemoveAll(i => i.Equals(-2147483647));
            Value.RemoveAll(i => i.Equals(-2147483647));
            X.RemoveAll(i => i.Equals(-1.0E32));
            Y.RemoveAll(i => i.Equals(-1.0E32));
            Value.RemoveAll(i => i.Equals(-1.0E32));
            //Find the edges of the total grid area
            int lengthX = Convert.ToInt32(Math.Ceiling(((X.Max() - X.Min()) / Globals.cellSize)));
            if (((X.Max() - X.Min()) % Globals.cellSize) == 0) //if the division is perfect then we need to add one
            {
                lengthX += 1;
            }
            int lengthY = Convert.ToInt32(Math.Ceiling(((Y.Max() - Y.Min()) / Globals.cellSize)));
            if (((Y.Max() - Y.Min()) % Globals.cellSize) == 0) //if the division is perfect then we need to add one
            {
                lengthY += 1;
            }

            double[,] tempX = new double[lengthX, lengthY];
            double[,] tempY = new double[lengthX, lengthY];
            double[,] tempV = new double[lengthX, lengthY];
            int[,] tempF = new int[lengthX, lengthY];

            double minX = X.Min();
            double minY = Y.Min();

            double[,] xposit = new double[lengthX, lengthY];
            double[,] yposit = new double[lengthX, lengthY];

            for (int k = 0; k < X.Count; k++)
            {
                CSYS.ProgUpdateL(k, X.Count);
                //ints, will round down, therefore the xPos will be in the grid as using the bottom left point as the reference.
                int xPos = Convert.ToInt32(Math.Floor((X[k] - minX) / Globals.cellSize));
                int yPos = Convert.ToInt32(Math.Floor((Y[k] - minY) / Globals.cellSize));
                xposit[xPos, yPos] = (xPos * Globals.cellSize) + minX; //find bottom left position
                yposit[xPos, yPos] = (yPos * Globals.cellSize) + minY; //find bottom left position
                tempX[xPos, yPos] += X[k] - xposit[xPos, yPos]; //Add the x pos of that reading to the cell. Essentially finding how far away from the bottom left it is.
                tempY[xPos, yPos] += Y[k] - yposit[xPos, yPos]; //Add the y pos of that reading to the cell.
                //tempX[xPos, yPos] = ((xPos * Globals.cellSize) + minX + (Globals.cellSize / 2)); //Essentially round the position to the center of the cell
                //tempY[xPos, yPos] = ((yPos * Globals.cellSize) + minY + (Globals.cellSize / 2)); //Essentially round the position to the center of the cell
                tempV[xPos, yPos] += Value[k]; //Add the value of that reading to the cell.
                tempF[xPos, yPos]++; //Account for how many readings have been assigned to the cell.
            }
            CSYS.Progress(0);

            int[, ,] tempclose = new int[tempX.GetUpperBound(0) + 1, tempX.GetUpperBound(1) + 1, 8];
            int[, , ,] tempclose1 = new int[tempX.GetUpperBound(0) + 1, tempX.GetUpperBound(1) + 1, 8, 2];

            cellData gridedData = new cellData(tempX, tempY, tempV, tempF, tempclose, tempclose1);

            //Check through all grid cells, and if a cell has more than one reading in it, average the value over the number of readings.
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    if (gridedData.Flag[i, j] >= 1)
                    {
                        gridedData.X[i, j] = (gridedData.X[i, j] / gridedData.Flag[i, j]) + xposit[i, j];
                        gridedData.Y[i, j] = (gridedData.Y[i, j] / gridedData.Flag[i, j]) + yposit[i, j];
                        gridedData.Value[i, j] = gridedData.Value[i, j] / gridedData.Flag[i, j];
                        gridedData.Flag[i, j] = 1; //Set it back to 1, showing it is populated by a "true" value
                    }
                }
            }
            //Now go through all cells, and assign values to the ones that have no data currently, or determine if they are too far away from real data to use. 
            //During this process we will also find all closest data to each cell which will be information needed when normalizing.
            CSYS.Progress(1);
            CSYS.ProgName("Gridding Pt 2", 0);
            for (int i = 0; i < lengthX; i++)
            {
                CSYS.ProgUpdateL(i, lengthX);
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
            CSYS.Progress(0);
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

            cellData gridedDataDerivIterate = new cellData(tempX2, tempY2, tempV2, tempF2);
            cellData realReplace = new cellData(tempX3, tempY3, tempV3, tempF3);
            cellData previousGrid = new cellData(tempX5, tempY5, tempV5, tempF5);

            CSYS.Progress(1);
            CSYS.ProgName("Interpolating", 0);

            int looping = 0;
            int currentLoop = 0;

            double previousMean = 0;
            double previousMedian = 0;

            int numStops = 0;

            while (looping == 0)
            {
                CSYS.ProgUpdateL(currentLoop, Globals.maxLoop);
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
                double[,] smoothGrid = new double[lengthX, lengthY];
                double[,] m1 = new double[lengthX, lengthY];
                double[,] v11 = new double[lengthX, lengthY];
                double[,] v12 = new double[lengthX, lengthY];

                //first smooth the entire grid to remove noise issues
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedDataDerivIterate.Flag[i, j] == -1) //If the data is outside of the grid, then don't use it
                        {
                        }
                        else
                        {
                            List<double> points = new List<double>();
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 0, 0], gridedData.close1[i, j, 0, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 1, 0], gridedData.close1[i, j, 1, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 2, 0], gridedData.close1[i, j, 2, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 3, 0], gridedData.close1[i, j, 3, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 4, 0], gridedData.close1[i, j, 4, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 5, 0], gridedData.close1[i, j, 5, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 6, 0], gridedData.close1[i, j, 6, 1]]);
                            points.Add(gridedDataDerivIterate.Value[gridedData.close1[i, j, 7, 0], gridedData.close1[i, j, 7, 1]]);
                            points.Add(gridedDataDerivIterate.Value[i, j]);

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

                            double sumAlpha = 0;
                            for (int k = 0; k < 9; k++)
                            {
                                sumAlpha = sumAlpha + points[k];
                            }
                            
                            smoothGrid[i, j] = sumAlpha / 9;
                        }
                    }
                }

                //now calculate the structure tensor and get the eigenvector for directionality information
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedDataDerivIterate.Flag[i, j] == -1) //If the data is outside of the grid, then don't use it
                        {
                        }
                        else
                        {
                            double Ix = new double();
                            double Iy = new double();
                            if (i == 0)
                            {
                                Ix = smoothGrid[i + 1, j] - smoothGrid[i, j];
                            }
                            else if (i == lengthX - 1)
                            {
                                Ix = smoothGrid[i, j] - smoothGrid[i - 1, j];
                            }
                            else
                            {
                                Ix = 0.5 * (smoothGrid[i + 1, j] - smoothGrid[i - 1, j]);
                            }
                            if (j == 0)
                            {
                                Iy = smoothGrid[i, j + 1] - smoothGrid[i, j];
                            }
                            else if (j == lengthY - 1)
                            {
                                Iy = smoothGrid[i, j] - smoothGrid[i, j - 1];
                            }
                            else
                            {
                                Iy = 0.5 * (smoothGrid[i, j + 1] - smoothGrid[i, j - 1]);
                            }

                            //create the structure matrix
                            double[,] S = new double[2, 2];
                            S[0, 0] = Ix * Ix;
                            S[0, 1] = Ix * Iy;
                            S[1, 0] = Iy * Ix;
                            S[1, 1] = Iy * Iy;

                            //calculate eigenvalue for trend determination
                            m1[i,j] = 0.5*(S[0,1] + S[1,1] + Math.Sqrt((Math.Pow(S[0,0]-S[1,1],2)) + (4*Math.Pow(S[0,1],2))));

                            if (S[0, 1] > 0)
                            {
                                if (S[0, 0] == S[1, 1])
                                {
                                    //theta = 1/4 pi
                                    double theta = 0.25 * Math.PI;
                                    v12[i, j] = Math.Cos(theta);
                                    v11[i, j] = -1 * Math.Sin(theta);
                                }
                                else
                                {
                                    //0 < theta < 1/2 pi
                                    double theta = (Math.Atan(2 * S[0, 1] / (S[0, 0] - S[1, 1])) / 2);
                                    if (S[0, 0] < S[1, 1])
                                    {
                                        v11[i, j] = -1 * Math.Abs(Math.Cos(theta));
                                        v12[i, j] = Math.Abs(Math.Sin(theta));
                                    }
                                    else
                                    {
                                        v12[i, j] = -1 * Math.Abs(Math.Cos(theta));
                                        v11[i, j] = Math.Abs(Math.Sin(theta));
                                    }
                                }
                            }
                            else if (S[0, 1] == 0)
                            {
                                if (S[0, 0] > S[1, 1])
                                {
                                    //theta = 0
                                    v12[i, j] = 1;
                                    v11[i, j] = 0;
                                }
                                else
                                {
                                    //theta = 1/2 pi
                                    v12[i, j] = 0;
                                    v11[i, j] = 1;
                                }
                            }
                            else
                            {
                                if (S[0, 0] == S[1, 1])
                                {
                                    //theta = 3/4 pi
                                    double theta = (3 / 4) * Math.PI;
                                    v12[i, j] = Math.Abs(Math.Cos(theta));
                                    v11[i, j] = Math.Abs(Math.Sin(theta));
                                }
                                else
                                {
                                    //1/2 pi < theta < pi
                                    double theta = Math.Atan(2 * S[0, 1] / (S[0, 0] - S[1, 1])) / 2;
                                    if (S[0, 0] < S[1, 1])
                                    {
                                        v11[i, j] = -1 * Math.Abs(Math.Cos(theta));
                                        v12[i, j] = -1 * Math.Abs(Math.Sin(theta));
                                    }
                                    else
                                    {
                                        v12[i, j] = -1 * Math.Abs(Math.Cos(theta));
                                        v11[i, j] = -1 * Math.Abs(Math.Sin(theta));
                                    }
                                }
                            }
                        }
                    }
                }

                //***********************************

                //*************TRENDING DETERMINATION
                double eigenAvg = new double();
                List<double> flatEigenVal = new List<double>();

                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedDataDerivIterate.Flag[i, j] != -1)
                        {
                            flatEigenVal.Add(m1[i, j]);
                        }
                    }
                }
                flatEigenVal.Sort(); //sort the list
                if (Globals.trendM == 0)
                {
                    eigenAvg = flatEigenVal[Convert.ToInt32(flatEigenVal.Count) - 1]; //trendM larger = less change during normalization
                }
                else
                {
                    double tempTrendM = 100 - Globals.trendM; //Globals.trendM larger = less change during normalization
                    eigenAvg = flatEigenVal[Convert.ToInt32(flatEigenVal.Count * (tempTrendM / 100))]; //therefore to make this work we take 100 - Globals.trendM
                }

                //Create "Trending" grid
                double[,] trendLOG = new double[lengthX, lengthY];
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedDataDerivIterate.Flag[i, j] == -1) //If the data is outside of the grid, then don't use it
                        {
                            trendLOG[i, j] = 0;
                        }
                        else
                        {
                            if (m1[i, j] >= eigenAvg)
                            {
                                trendLOG[i, j] = 1;
                            }
                            else
                            {
                                trendLOG[i, j] = m1[i, j] / eigenAvg; //the larger m1 is, the smaller trendLOG will be, and the less change that will occur during normalization
                            }
                        }
                    }
                }
                //***********************************

                //REAL DATA SCALING******************
                double[,] multiplierCells = new double[lengthX, lengthY];
                double[,] iMultCells = new double[lengthX, lengthY];

                //find the real data, and the multiplier for each real data cell
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        if (gridedData.Flag[i, j] == 1)
                        {
                            double tempMult = Math.Abs(gridedData.Value[i, j] / gridedDataDerivIterate.Value[i, j]);
                            multiplierCells[i, j] = tempMult;
                            iMultCells[i, j] = tempMult;
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
                            iMultCells[i, j] = -999999;
                        }
                        else //otherwise it is interpolated data, and therefore we must find the real data along lines of isotropy
                        {
                            bool failed = false;
                            bool multFind = false;
                            double iMult = 0;
                            int multI = 0;
                            int multJ = 0;
                            double eigenDevi = 0;
                            double eigenSide = 1;
                            double tenD = Math.PI * Globals.angleSearch / 180;

                            while (multFind == false) //have we found any multiplier
                            {
                                double origXD = 0;
                                double origYD = 0;
                                //We need to determine the direction to search. First we can just use the eigenvector result, however if that fails we must begin searching elsewhere.
                                if (eigenDevi > 0)
                                {
                                    //instead let's find the direction of the eigenvector, and move the angle by theta degrees
                                    double origAng = Math.Atan(v12[i, j] / v11[i, j]); //measuring from the x-axis, so range is -90 -> +90
                                    double newAng = 0;

                                    if (Math.Abs(v11[i, j]) > Math.Abs(v12[i, j])) //if the initial x direction is stronger, then we will first search more towards the x-axis. Also means that if it is 0 degrees, we just assume one direction.
                                    {
                                        if (eigenDevi % 2 == 0) //then we are even, and therefore have already tried the x direction first
                                        {
                                            if (v11[i,j] > 0) //if it's positive, then we need to add the angle to move towards the y-axis
                                            {
                                                newAng = origAng + (eigenSide * tenD);
                                            }
                                            else
                                            {
                                                newAng = origAng - (eigenSide * tenD);
                                            }
                                            eigenSide++;
                                        }
                                        else //it is odd, and it is time to check the x direction
                                        {
                                            if (v11[i, j] > 0) //if it's positive, then we need to subtract the angle to move towards the x-axis
                                            {
                                                newAng = origAng - (eigenSide * tenD);
                                            }
                                            else
                                            {
                                                newAng = origAng + (eigenSide * tenD);
                                            }
                                        }
                                    }
                                    else if (Math.Abs(v11[i, j]) < Math.Abs(v12[i, j])) //if the initial y direction is stronger, then we will first search more towards the y-axis. Also means that if it is -90 or 90 degrees, we just assume one direction.
                                    {
                                        if (eigenDevi % 2 == 0) //then we are even, and therefore have already tried the y direction first
                                        {
                                            if (v12[i, j] > 0) //if it's positive, then we need to subtract the angle to move towards the x-axis
                                            {
                                                newAng = origAng - (eigenSide * tenD);
                                            }
                                            else
                                            {
                                                newAng = origAng + (eigenSide * tenD);
                                            }
                                            eigenSide++;
                                        }
                                        else //it is odd, and it is time to check the y direction
                                        {
                                            if (v12[i, j] > 0) //if it's positive, then we need to add the angle to move towards the y-axis
                                            {
                                                newAng = origAng + (eigenSide * tenD);
                                            }
                                            else
                                            {
                                                newAng = origAng - (eigenSide * tenD);
                                            }
                                        }
                                    }
                                    else //then they are equal, and we started at 45 degrees. Arbitrarily I will say we first search more towards the x-axis. May be worthwhile to change at a later time.
                                    {
                                        if (eigenDevi % 2 == 0) //then we are even, and therefore have already tried the x direction first
                                        {
                                            if (v11[i, j] > 0) //if it's positive, then we need to add the angle to move towards the y-axis
                                            {
                                                newAng = origAng + (eigenSide * tenD);
                                            }
                                            else
                                            {
                                                newAng = origAng - (eigenSide * tenD);
                                            }
                                            eigenSide++;
                                        }
                                        else //it is odd, and it is time to check the x direction
                                        {
                                            if (v11[i, j] > 0) //if it's positive, then we need to subtract the angle to move towards the x-axis
                                            {
                                                newAng = origAng - (eigenSide * tenD);
                                            }
                                            else
                                            {
                                                newAng = origAng + (eigenSide * tenD);
                                            }
                                        }
                                    }
   
                                    //now that we have a new angle, we need to give our new x and y distances
                                    if (Math.Abs(newAng) < (Math.PI / 4) || Math.Abs(newAng) > (3 * Math.PI / 4)) //then we are a stronger x direction
                                    {
                                        //normalize such that x is 1, then multiply by 1.1
                                        origXD = 1.1;
                                        origYD = Math.Abs(Math.Tan(newAng)) * 1.1;
                                        multI = 1;
                                    }
                                    else if (Math.Abs(newAng) == (Math.PI / 4) || Math.Abs(newAng) == (3 * Math.PI / 4))
                                    {
                                        origXD = 1.1;
                                        origYD = 1.1;
                                        multI = 1;
                                    }
                                    else
                                    {
                                        //normalize such that y is 1, then multiply by 1.1
                                        origXD = Math.Abs((1/Math.Tan(newAng))) * 1.1;
                                        origYD = 1.1;
                                        multI = 1;
                                    }
                                    if (newAng < 0)
                                    {
                                        multJ = -1;
                                    }
                                    else
                                    {
                                        multJ = 1;
                                    }                                    
                                }
                                else
                                {
                                    //normalize such that the large of the two eigenvectors is 1, then multiply by 1.1
                                    if (Math.Abs(v11[i, j]) > Math.Abs(v12[i, j]))
                                    {
                                        origXD = 1.1; //begin at 1.1 cells
                                        origYD = (Math.Abs(v12[i, j]) / Math.Abs(v11[i, j])) * 1.1;
                                    }
                                    else
                                    {
                                        origXD = (Math.Abs(v11[i, j]) / Math.Abs(v12[i, j])) * 1.1;
                                        origYD = 1.1; //begin at 1.1 cells
                                    }
                                    multI = Math.Sign(v11[i, j]);
                                    multJ = Math.Sign(v12[i, j]);
                                }

                                //Now with the x and y found, we need to look in both positive and negative directions, and find the locations of the first real data cells we run into.
                                double searchStep = 1; //start at the point where it would be in the next cell if looking directly along axes
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
                                while (searching == true) //begin searching with current direction
                                {
                                    //find the distance we are looking
                                    double currentStep = 1 + (Globals.searchStepSize * searchStep);
                                    double tempXD = origXD * currentStep;
                                    double tempYD = origYD * currentStep;

                                    //now we need to know which direction we are looking, and add the current distance to i and j (current location)
                                    if (searchPos == false) //we have not found a real data cell in the positive direction yet
                                    {
                                        int tempI = Convert.ToInt32(multI * Math.Floor(tempXD));
                                        int tempJ = Convert.ToInt32(multJ * Math.Floor(tempYD));
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
                                            double XPM = (multI * tempXD) - (tempI + 0.5);
                                            double YPM = (multJ * tempYD) - (tempJ + 0.5);

                                            for (int k = 0; k < 8; k++)
                                            {
                                                if (gridedData.Flag[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] == 1) //this cell is real
                                                {
                                                    if (gridedData.close1[i + tempI, j + tempJ, k, 0] == (i + tempI) && gridedData.close1[i + tempI, j + tempJ, k, 1] == (j + tempJ)) //and not a repeat value
                                                    {
                                                    }
                                                    else
                                                    {
                                                        int wayX = 0;
                                                        int wayY = 0;
                                                        //basically just determine which direction this is. Not the best way to do it, but should work.
                                                        if (k == 7 || k == 0 || k == 1)
                                                        {
                                                            wayX = -1;
                                                        }
                                                        else if (k == 3 || k == 4 || k == 5)
                                                        {
                                                            wayX = 1;
                                                        }
                                                        if (k == 1 || k == 2 || k == 3)
                                                        {
                                                            wayY = 1;
                                                        }
                                                        else if (k == 5 || k == 6 || k == 7)
                                                        {
                                                            wayY = -1;
                                                        }
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
                                            }

                                            iT1 = tempI;
                                            jT1 = tempJ;
                                            searchPos = true;
                                            posGood = true;
                                        }
                                    }
                                    if (searchNeg == false) //we have not found a real data cell in the negative direction yet
                                    {
                                        int tempI = Convert.ToInt32(-multI * Math.Floor(tempXD));
                                        int tempJ = Convert.ToInt32(-multJ * Math.Floor(tempYD));
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
                                            double XPM = (-multI * tempXD) - (tempI + 0.5);
                                            double YPM = (-multJ * tempYD) - (tempJ + 0.5);

                                            for (int k = 0; k < 8; k++)
                                            {
                                                if (gridedData.Flag[gridedData.close1[i + tempI, j + tempJ, k, 0], gridedData.close1[i + tempI, j + tempJ, k, 1]] == 1) //this cell is real
                                                {
                                                    if (gridedData.close1[i + tempI, j + tempJ, k, 0] == (i + tempI) && gridedData.close1[i + tempI, j + tempJ, k, 1] == (j + tempJ)) //and not a repeat value
                                                    {
                                                    }
                                                    else
                                                    {
                                                        int wayX = 0;
                                                        int wayY = 0;
                                                        //basically just determine which direction this is. Not the best way to do it, but should work.
                                                        if (k == 7 || k == 0 || k == 1)
                                                        {
                                                            wayX = -1;
                                                        }
                                                        else if (k == 3 || k == 4 || k == 5)
                                                        {
                                                            wayX = 1;
                                                        }
                                                        if (k == 1 || k == 2 || k == 3)
                                                        {
                                                            wayY = 1;
                                                        }
                                                        else if (k == 5 || k == 6 || k == 7)
                                                        {
                                                            wayY = -1;
                                                        }
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
                                        if (Globals.searchStepSize * searchStep >= (Globals.interpDist / Globals.cellSize) - 1) //then we have not found any real data cell within the user-defined interpolation distance
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
                                    eigenDevi++; //this direction did not work, so search new angle
                                    if ((eigenSide+1) * Globals.angleSearch >= 180) //then we've checked every direction
                                    {
                                        failed = true;
                                        multFind = true;
                                    }
                                }
                            }
                            if (failed == true)
                            {
                                realReplace.Value[i, j] = -999999; //failed data
                                realReplace.Flag[i, j] = -1; //outside of grid
                                iMultCells[i, j] = -999999;
                            }
                            else
                            {
                                iMultCells[i, j] = iMult;
                            }

                        }
                    }
                }

                //Apply the normalization
                for (int i = 0; i < lengthX; i++)
                {
                    for (int j = 0; j < lengthY; j++)
                    {
                        //Not sure if I need these here
                        if (realReplace.Flag[i, j] == 1)
                        {
                        }
                        else if (realReplace.Flag[i, j] == -1)
                        {
                        }
                        else
                        {
                            double tempMR = gridedDataDerivIterate.Value[i, j] + (((gridedDataDerivIterate.Value[i, j] * iMultCells[i, j]) - gridedDataDerivIterate.Value[i, j]) * trendLOG[i, j]);
                            if (tempMR > maxVal) //do not let the scaled data become greater than the max value found in the raw data
                            {
                                tempMR = maxVal;
                            }
                            realReplace.Value[i, j] = tempMR; //replace with scaled data
                            realReplace.Flag[i, j] = 0; //not real
                        }
                    }
                }
                //***********************************

                //Stopping criteria check************
                if (Globals.autoStop == true)
                {
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
                }
                //***********************************
                currentLoop++;
                if (Globals.autoStop == true && numStops >= 3)
                {
                    looping = 1;
                }
                if (currentLoop == Globals.maxLoop) //Hard stop of max iterations
                {
                    looping = 1;
                }
            }
            CSYS.Progress(0);
            //***********************************

            //**************************SUBSAMPLE
            CSYS.Progress(1);
            CSYS.ProgName("Resampling Pt 1", 0);
            //Find the edges of the total grid area
            int lengthXF = Convert.ToInt32(Math.Ceiling(((X.Max() - X.Min()) / Globals.cellSizeF)));
            if (((X.Max() - X.Min()) % Globals.cellSizeF) == 0) //if the division is perfect then we need to add one
            {
                lengthXF += 1;
            }
            int lengthYF = Convert.ToInt32(Math.Ceiling(((Y.Max() - Y.Min()) / Globals.cellSizeF)));
            if (((Y.Max() - Y.Min()) % Globals.cellSizeF) == 0) //if the division is perfect then we need to add one
            {
                lengthYF += 1;
            }

            double[,] tempXF = new double[lengthXF, lengthYF];
            double[,] tempYF = new double[lengthXF, lengthYF];
            double[,] tempVF = new double[lengthXF, lengthYF];
            int[,] tempFF = new int[lengthXF, lengthYF];
            double[,] xpositf = new double[lengthXF, lengthYF];
            double[,] ypositf = new double[lengthXF, lengthYF];

            //Make sure we only use original data at first, and get those cells completed. After that we assign any data to the remaining cells.
            for (int k = 0; k < X.Count; k++)
            {
                CSYS.ProgUpdateL(k, X.Count);
                //ints, will round down, therefore the xPos will be in the grid as using the bottom left point as the reference.
                int xPos = Convert.ToInt32(Math.Floor((X[k] - minX) / Globals.cellSizeF));
                int yPos = Convert.ToInt32(Math.Floor((Y[k] - minY) / Globals.cellSizeF));
                xpositf[xPos, yPos] = (xPos * Globals.cellSizeF) + minX; //find bottom left position
                ypositf[xPos, yPos] = (yPos * Globals.cellSizeF) + minY; //find bottom left position
                tempXF[xPos, yPos] += X[k] - xpositf[xPos, yPos]; //Add the x pos of that reading to the cell.
                tempYF[xPos, yPos] += Y[k] - ypositf[xPos, yPos]; //Add the y pos of that reading to the cell.
                //tempXF[xPos, yPos] = ((xPos * Globals.cellSizeF) + minX + (Globals.cellSizeF / 2)); //Essentially round the position to the center of the cell
                //tempYF[xPos, yPos] = ((yPos * Globals.cellSizeF) + minY + (Globals.cellSizeF / 2)); //Essentially round the position to the center of the cell
                if (minVal < 0)
                {
                    tempVF[xPos, yPos] += Value[k] + Math.Abs(minVal); //Add the value of that reading to the cell.
                }
                else
                {
                    tempVF[xPos, yPos] += Value[k]; //Add the value of that reading to the cell.
                }
                tempFF[xPos, yPos]++; //Account for how many readings have been assigned to the cell.
            }

            //Now go through all original grid cells and assign them to new grid cells that have no data currently.
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthY; j++)
                {
                    //ints, will round down, therefore the xPos will be in the grid as using the bottom left point as the reference.
                    int xPos = Convert.ToInt32(Math.Floor((realReplace.X[i, j] - minX) / Globals.cellSizeF));
                    int yPos = Convert.ToInt32(Math.Floor((realReplace.Y[i, j] - minY) / Globals.cellSizeF));

                    if (tempFF[xPos, yPos] <= 0 && realReplace.Value[i, j] != -999999) //if this cell isn't a real data cell and isn't bad data, then we go ahead
                    {
                        //xpositf[xPos, yPos] = (xPos * Globals.cellSizeF) + minX; //find bottom left position
                        //ypositf[xPos, yPos] = (yPos * Globals.cellSizeF) + minY; //find bottom left position
                        //tempXF[xPos, yPos] += realReplace.X[i, j] - xpositf[xPos, yPos]; //Add the x pos of that reading to the cell.
                        //tempYF[xPos, yPos] += realReplace.Y[i, j] - ypositf[xPos, yPos]; //Add the y pos of that reading to the cell.
                        tempXF[xPos, yPos] = ((xPos * Globals.cellSizeF) + minX + (Globals.cellSizeF / 2)); //Essentially round the position to the center of the cell
                        tempYF[xPos, yPos] = ((yPos * Globals.cellSizeF) + minY + (Globals.cellSizeF / 2)); //Essentially round the position to the center of the cell
                        tempVF[xPos, yPos] += realReplace.Value[i, j];
                        tempFF[xPos, yPos]--;
                    }
                }
            }

            CSYS.Progress(0);

            CSYS.Progress(1);
            CSYS.ProgName("Resampling Pt 2", 0);

            cellData finalData = new cellData(tempXF, tempYF, tempVF, tempFF);

            //Check through all grid cells, and if a cell has more than one reading in it, average the value over the number of readings.
            for (int i = 0; i < lengthXF; i++)
            {
                CSYS.ProgUpdateL(i, lengthXF);
                for (int j = 0; j < lengthYF; j++)
                {
                    if (finalData.Flag[i, j] >= 1) //real data cell
                    {
                        finalData.X[i, j] = (finalData.X[i, j] / finalData.Flag[i, j]) + xpositf[i, j];
                        finalData.Y[i, j] = (finalData.Y[i, j] / finalData.Flag[i, j]) + ypositf[i, j];
                        finalData.Value[i, j] = finalData.Value[i, j] / finalData.Flag[i, j];
                        finalData.Flag[i, j] = 1; //the position of real data vs interpolated data no longer matters, so just set all useable data to 1
                    }
                    else if (finalData.Flag[i, j] == 0) //there was no data added to this cell, and therefore it is outside of the range we can use
                    {
                        finalData.Value[i, j] = -999999;
                        finalData.Flag[i, j] = -1;
                    }
                    else //interpolated data cell
                    {
                        //finalData.X[i, j] = (finalData.X[i, j] / (-1 * finalData.Flag[i, j])) + xpositf[i, j];
                        //finalData.Y[i, j] = (finalData.Y[i, j] / (-1 * finalData.Flag[i, j])) + ypositf[i, j];
                        finalData.Value[i, j] = finalData.Value[i, j] / (-1 * finalData.Flag[i, j]); //must multiply by -1 to remove the negative flag values
                        finalData.Flag[i, j] = 1; //the position of real data vs interpolated data no longer matters, so just set all useable data to 1 (leaving -1 as is)
                    }
                }
            }
            CSYS.Progress(0);
            //***********************************

            //*******************SUBTRACT MIN VAL
            if (minVal < 0)
            {
                //Subtract off the min val found earlier
                for (int i = 0; i < lengthXF; i++)
                {
                    for (int j = 0; j < lengthYF; j++)
                    {
                        if (finalData.Flag[i, j] != -1)
                        {
                            finalData.Value[i, j] = finalData.Value[i, j] - Math.Abs(minVal);
                        }
                    }
                }
            }
            //***********************************

            //*****************************OUTPUT
            CSYS.Progress(1);
            CSYS.ProgName("Writing ASCII grid file.", 0);

            Globals.outputFile = Globals.inputFile + "-" + Globals.cellSize + "m" + Globals.cellSizeF + "m" + Globals.interpDist + "m" + currentLoop + "x" + Globals.trendM + "t" + Globals.angleSearch + "Th" + ".txt";
            System.IO.StreamWriter myfile2 = new System.IO.StreamWriter(Globals.outputFile, true);
            //file. setprecision(10); //Set the precision of the output data to 10 sig digs. Do this to not allow incorrect values in sci notation.

            double botleftX = minX + Globals.cellSizeF / 2;
            double botleftY = minY + Globals.cellSizeF / 2;

            //info lines
            myfile2.WriteLine("Grid created using Tomas Naprstek and Richard S. Smith's interpolation method. Generated by Geosoft's Oasis Montaj.");
            myfile2.WriteLine("Using Geosoft's Oasis Montaj, enter the following information into: 'Grid and Image'->'Utilities'->'Import ASCII Grid...'.");
            myfile2.WriteLine("***************************************");
            myfile2.WriteLine("ASCII Grid File: " + Globals.outputFile);
            myfile2.WriteLine("Output grid file (*.grd): " + Globals.inputFile + "-" + Globals.cellSize + "m" + Globals.cellSizeF + "m" + Globals.interpDist + "m" + currentLoop + "x" + Globals.trendM + "t" + Globals.angleSearch + "Th");
            myfile2.WriteLine("Number of ASCII lines to skip: 19");
            myfile2.WriteLine("Numper of points in each row: " + lengthYF);
            myfile2.WriteLine("Number of rows: " + lengthXF);
            myfile2.WriteLine("Row orientation: Left bottom to top");
            myfile2.WriteLine("Dummy value: -999999");
            myfile2.WriteLine("X grid point separation: " + Globals.cellSizeF);
            myfile2.WriteLine("Y grid point separation: " + Globals.cellSizeF);
            myfile2.WriteLine("X location of bottom left point: " + botleftX);
            myfile2.WriteLine("Y location of bottom left point: " + botleftY);
            myfile2.WriteLine("Grid rotation angle, CCW degrees: 0");
            myfile2.WriteLine("# of iterations required: " + currentLoop);
            myfile2.WriteLine("Trending Factor: " + Globals.trendM);
            myfile2.WriteLine("Theta: " + Globals.angleSearch);
            myfile2.WriteLine("***************************************");
            //loop for making the ascii grid file
            for (int i = 0; i < lengthXF; i++)
            {
                CSYS.ProgUpdateL(i, lengthXF);
                string tempLine = "";
                for (int j = 0; j < lengthYF; j++)
                {
                    tempLine = tempLine + finalData.Value[i, j] + "\t";
                }
                myfile2.WriteLine(tempLine);
            }
            myfile2.Close();
            CSYS.Progress(0);
            MessageBox.Show("Interpolation complete! ASCII grid file generated.");
            //***********************************

            //*****************CALL READASCIIGRID
            //int successGX = CSYS.iRunGX("readasciigrid");
            //***********************************

            return;
        }
    }
}
