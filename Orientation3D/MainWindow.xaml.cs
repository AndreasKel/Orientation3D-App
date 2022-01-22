//Electrobird
//(c)Copyright 2022 All Rights Reserved
//-----------------------------------------------------------------------------------------------------------------------------------
//license:          MIT
//file name:        MainWindow.xaml.cs
//language:         C#
//environment:      .Net Core
//functionality:    uses data from an accelerometer and a gyroscope to estimate and visualise the current orientation with quaternions
//====================================================================================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.IO.Ports;
using System.Diagnostics;
using System.ComponentModel;

namespace Orientation3D
{
    public partial class MainWindow : Window
    {
        private float _selectedFilter;

        cOrientation Filters = new cOrientation(0.015f);                                        //Initialise an instance of Filters class -> Specify the sample rate
        
        SerialPort myPort = new SerialPort();                                                               //Instance of SerialPort class
        Grid myGrid = new Grid();                                                                           //Instance of the Main Grid - main child of the window
        TextBlock txtQx = new TextBlock();                                                                  //Instance of Textblock for qx 
        TextBlock txtQy = new TextBlock();                                                                  //Instance of Textblock for qy 
        TextBlock txtQz = new TextBlock();                                                                  //Instance of Textblock for qz 
        TextBlock txtQw = new TextBlock();                                                                  //Instance of Textblock for qw 
        TextBox txtPortName = new TextBox();                                                                //Instance of Textbox for port name 
        ComboBox cbxBaudRate = new ComboBox();
        ComboBox cbxFilter = new ComboBox();
        Model3DGroup myModel3DGroup = new Model3DGroup();                                                   //Instance of model group that holds all the 3D parts of the aircraft
        Quaternion mySerialPortQuaternion = new Quaternion();                                               //Structure of quaternion that is used to rotate the aircraft when the filter is applied
        RotateTransform3D myDroneTransform3D = new RotateTransform3D();                                     //Specifies a rotation transformation

        BackgroundWorker bgWorker = new BackgroundWorker();                                                 //Executes an operation on a separate thread

        public MainWindow()
        {
            InitializeComponent();
            GridLayout();
            FlightDirection(1, 0, 0, 0);
            GridElemets();

            //Background Thread
            bgWorker.DoWork += bgWorker_DoWork;
            bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
        }

        //Main Grid
        private void GridLayout()
        {
            myGrid.Width = 800;
            myGrid.Height = 450;

            myGrid.HorizontalAlignment = HorizontalAlignment.Left;

            myGrid.VerticalAlignment = VerticalAlignment.Top;

            myGrid.ShowGridLines = false;

            ColumnDefinition gridCol1 = new ColumnDefinition();
            gridCol1.Width = new GridLength(150);
            ColumnDefinition gridCol2 = new ColumnDefinition();
            myGrid.ColumnDefinitions.Add(gridCol1);
            myGrid.ColumnDefinitions.Add(gridCol2);
            this.Content = myGrid;
        }

        //Elements on the main Grid. Text boxes that hold information about current quaternion state etc
        private void GridElemets()
        {
            //Textbox holding the title "Quaternion"
            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = "Quaternion";
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            txtBlock1.Foreground = new SolidColorBrush(Colors.AntiqueWhite);
            Thickness myThickness = new Thickness();
            myThickness.Left = 50;
            myThickness.Top = 10;
            txtBlock1.Margin = myThickness;
            txtBlock1.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(txtBlock1, 0);
            Grid.SetColumn(txtBlock1, 0);

            //Textbox with the text information of the current state of qw
            txtQw.FontSize = 14;
            txtQw.FontWeight = FontWeights.Bold;
            txtQw.Foreground = new SolidColorBrush(Colors.Yellow);
            Thickness myThicknessQw = new Thickness();
            myThicknessQw.Left = 50;
            myThicknessQw.Top = 50;
            txtQw.Margin = myThicknessQw;
            txtQw.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(txtQw, 0);
            Grid.SetColumn(txtQw, 0);

            //Textbox with the text information of the current state of qx
            txtQx.FontSize = 14;
            txtQx.FontWeight = FontWeights.Bold;
            txtQx.Foreground = new SolidColorBrush(Colors.Red);
            Thickness myThicknessQx = new Thickness();
            myThicknessQx.Left = 50;
            myThicknessQx.Top = 100;
            txtQx.Margin = myThicknessQx;
            txtQx.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(txtQx, 0);
            Grid.SetColumn(txtQx, 0);

            //Textbox with the text information of the current state of qy
            txtQy.FontSize = 14;
            txtQy.FontWeight = FontWeights.Bold;
            txtQy.Foreground = new SolidColorBrush(Colors.Blue);
            Thickness myThicknessQy = new Thickness();
            myThicknessQy.Left = 50;
            myThicknessQy.Top = 150;
            txtQy.Margin = myThicknessQy;
            txtQy.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(txtQy, 0);
            Grid.SetColumn(txtQy, 0);

            //Textbox with the text information of the current state of qz
            txtQz.FontSize = 14;
            txtQz.FontWeight = FontWeights.Bold;
            txtQz.Foreground = new SolidColorBrush(Colors.Green);
            Thickness myThicknessQz = new Thickness();
            myThicknessQz.Left = 50;
            myThicknessQz.Top = 200;
            txtQz.Margin = myThicknessQz;
            txtQz.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(txtQz, 0);
            Grid.SetColumn(txtQz, 0);

            //Text box to define port name
            txtPortName.Text = "COM3";
            txtPortName.FontSize = 14;
            //txtPortName.FontWeight = FontWeights.Bold;
            txtPortName.Foreground = new SolidColorBrush(Colors.Black);
            txtPortName.Background = new SolidColorBrush(Colors.White);
            txtPortName.Width = 110;
            txtPortName.Height = 25;
            Thickness myThicknesstPortName = new Thickness();
            myThicknesstPortName.Left = 0;
            myThicknesstPortName.Top = 230;
            txtPortName.Margin = myThicknesstPortName;
            txtPortName.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(txtPortName, 0);
            Grid.SetColumn(txtPortName, 0);

            //Combobox for baud rate
            cbxBaudRate.FontSize = 10;
            cbxBaudRate.FontWeight = FontWeights.Bold;
            cbxBaudRate.Foreground = new SolidColorBrush(Colors.Gray);
            cbxBaudRate.Name = "ComboBox1";
            cbxBaudRate.Width = 110;
            cbxBaudRate.Height = 25;
            cbxBaudRate.IsEditable = true;
            cbxBaudRate.Text = "Select Baud Rate";
            cbxBaudRate.Items.Add("4800");
            cbxBaudRate.Items.Add("9600");
            cbxBaudRate.Items.Add("19200");
            cbxBaudRate.Items.Add("38400");
            cbxBaudRate.Items.Add("57600");
            cbxBaudRate.Items.Add("115200");
            cbxBaudRate.Items.Add("250000");
            Thickness myThicknesscbx1 = new Thickness();
            myThicknesscbx1.Left = 0;
            myThicknesscbx1.Top = 290;
            cbxBaudRate.Margin = myThicknesscbx1;
            cbxBaudRate.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(cbxBaudRate, 0);
            Grid.SetColumn(cbxBaudRate, 0);

            //Combobox for filter
            cbxFilter.FontSize = 10;
            cbxFilter.FontWeight = FontWeights.Bold;
            cbxFilter.Foreground = new SolidColorBrush(Colors.Gray);
            cbxFilter.Name = "ComboBox2";
            cbxFilter.Width = 110;
            cbxFilter.Height = 25;
            cbxFilter.IsEditable = true;
            cbxFilter.Text = "Select Filter";
            cbxFilter.Items.Add("Kalman Filter");
            cbxFilter.Items.Add("Kalman Filter with Bias");
            cbxFilter.Items.Add("Complementary Filter");
            Thickness myThicknesscbx2 = new Thickness();
            myThicknesscbx2.Left = 0;
            myThicknesscbx2.Top = 260;
            cbxFilter.Margin = myThicknesscbx2;
            cbxFilter.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(cbxFilter, 0);
            Grid.SetColumn(cbxFilter, 0);

            //Add the elements on the Grid
            myGrid.Children.Add(OpenComms);
            myGrid.Children.Add(txtBlock1);
            myGrid.Children.Add(txtQx);
            myGrid.Children.Add(txtQy);
            myGrid.Children.Add(txtQz);
            myGrid.Children.Add(txtQw);
            myGrid.Children.Add(cbxBaudRate);
            myGrid.Children.Add(cbxFilter);
            myGrid.Children.Add(txtPortName);
        }

        public void FlightDirection(float wi, float xi, float yi, float zi)
        {
            Viewport3D myViewport3D = new Viewport3D();                 //Hosts/renders a 3D model into our WPF application
            Model3DGroup myModel3DGroupAxes = new Model3DGroup();       //Enables a number of 3-D models as a unit to group the Axis models.
            ModelVisual3D myModelVisual3DAxes = new ModelVisual3D();    //Provides services and properties that are common to all visual objects in the group. In this instance the group is axes only.
            ModelVisual3D myModelVisual3D = new ModelVisual3D();        //Provides services and properties that are common to all visual objects in the group. In this instance the group is the aircraft model.

            //Points to draw the 3 Axes X, Y, Z
            Point3D myOrigin = new Point3D(0, 0, 0);
            Point3D myXAxis = new Point3D(4, 0, 0);
            Point3D myYAxis = new Point3D(0, 4, 0);
            Point3D myZAxis = new Point3D(0, 0, 4);

            //Points to draw a cuboid that represents the fuselage of the aircraft
            Point3D fuselage_p1 = new Point3D(0, 0, 3);
            Point3D fuselage_p2 = new Point3D(0, 0, -3);

            //clears the group objects
            myModel3DGroup.Children.Clear();

            //Identity quaternion qw = 1, qx = 0, qy = 0, qz = 0
            Quaternion IdentityQuaternion = new Quaternion(0, 0, 0, 1);
            QuaternionRotation3D myIdentityQuaternion3D = new QuaternionRotation3D(IdentityQuaternion);

            //Draws the 3 Axes X, Y, Z
            DrawCuboid(myModel3DGroupAxes, myIdentityQuaternion3D, myOrigin, myXAxis, Brushes.Red, 0.05f);
            DrawCuboid(myModel3DGroupAxes, myIdentityQuaternion3D, myOrigin, myYAxis, Brushes.Blue, 0.05f);
            DrawCuboid(myModel3DGroupAxes, myIdentityQuaternion3D, myOrigin, myZAxis, Brushes.Yellow, 0.05f);

            //Creates the Camera
            PerspectiveCamera myPCamera = new PerspectiveCamera();
            myPCamera.Position = new Point3D(0, 0, 12);
            myPCamera.LookDirection = new Vector3D(0, 0, -1);
            myPCamera.FieldOfView = 80;

            //Rotate the camera to the an angle of -45 degrees
            RotateTransform3D myRotatecamera = new RotateTransform3D();
            AxisAngleRotation3D myAxisAngleRotationcamera = new AxisAngleRotation3D();
            myAxisAngleRotationcamera.Axis = new Vector3D(1, 0, 0);
            myAxisAngleRotationcamera.Angle = -45;
            myRotatecamera.Rotation = myAxisAngleRotationcamera;
            myPCamera.Transform = myRotatecamera;

            //Directional Light
            DirectionalLight myDirectionalLight = new DirectionalLight();
            myDirectionalLight.Color = Colors.White;
            myDirectionalLight.Direction = new Vector3D(-0.61, -0.5, -0.61);

            // Asign the camera to the viewport
            myViewport3D.Camera = myPCamera;
            myModel3DGroupAxes.Children.Add(myDirectionalLight);

            //Create an initial rotation to the object.
            Quaternion endQuaternion = new Quaternion(xi, yi, zi, wi);
            QuaternionRotation3D myQuaternionRotation3D = new QuaternionRotation3D(endQuaternion);
            RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            myRotateTransform3D.Rotation = myQuaternionRotation3D;

            // Create the Fuselage
            DrawCuboid(myModel3DGroup, myQuaternionRotation3D, fuselage_p1, fuselage_p2, Brushes.SkyBlue, 1f);

            ////////////////
            /////Wings/////
            ///////////////
            #region Wings
            // triangles
            //Renders the geometry with the given material.
            GeometryModel3D surface7 = new GeometryModel3D();
            GeometryModel3D surface8 = new GeometryModel3D();
            GeometryModel3D surface9 = new GeometryModel3D();
            GeometryModel3D surface10 = new GeometryModel3D();
            GeometryModel3D surface11 = new GeometryModel3D();
            GeometryModel3D surface12 = new GeometryModel3D();

            //Triangle primitive for building a 3-D shape. Each instance represents a wing.
            MeshGeometry3D meshSurface7 = new MeshGeometry3D();
            MeshGeometry3D meshSurface8 = new MeshGeometry3D();
            MeshGeometry3D meshSurface9 = new MeshGeometry3D();
            MeshGeometry3D meshSurface10 = new MeshGeometry3D();
            MeshGeometry3D meshSurface11 = new MeshGeometry3D();
            MeshGeometry3D meshSurface12 = new MeshGeometry3D();

            //Create a collection of 3D points for each surface. Each surface represents a wing.
            //7th surface - bottom upright wing 
            Point3DCollection myPositionCollection7 = new Point3DCollection();
            myPositionCollection7.Add(new Point3D(0.1, 0.5, 0.5 - 2));
            myPositionCollection7.Add(new Point3D(0, 2.0, 0.0 - 2));
            myPositionCollection7.Add(new Point3D(0.1, 0.5, -0.5 - 2));
            myPositionCollection7.Add(new Point3D(-0.1, 0.5, 0.5 - 2));
            myPositionCollection7.Add(new Point3D(0, 2.0, 0.0 - 2));
            myPositionCollection7.Add(new Point3D(-0.1, 0.5, -0.5 - 2));

            myPositionCollection7.Add(new Point3D(0.1, 0.5, 0.5 - 2));
            myPositionCollection7.Add(new Point3D(0, 2.0, 0.0 - 2));
            myPositionCollection7.Add(new Point3D(-0.1, 0.5, 0.5 - 2));

            myPositionCollection7.Add(new Point3D(0.1, 0.5, -0.5 - 2));
            myPositionCollection7.Add(new Point3D(0, 2.0, 0.0 - 2));
            myPositionCollection7.Add(new Point3D(-0.1, 0.5, -0.5 - 2));
            meshSurface7.Positions = myPositionCollection7;

            //8th surface - bottom down wing
            Point3DCollection myPositionCollection8 = new Point3DCollection();
            myPositionCollection8.Add(new Point3D(0.1, -0.5, 0.5));
            myPositionCollection8.Add(new Point3D(0, -1, 0));
            myPositionCollection8.Add(new Point3D(0.1, -0.5, -0.5));
            myPositionCollection8.Add(new Point3D(-0.1, -0.5, 0.5));
            myPositionCollection8.Add(new Point3D(0, -1.0, 0));
            myPositionCollection8.Add(new Point3D(-0.1, -0.5, -0.5));

            myPositionCollection8.Add(new Point3D(0.1, -0.5, 0.5));
            myPositionCollection8.Add(new Point3D(0, -1.0, 0.0));
            myPositionCollection8.Add(new Point3D(-0.1, -0.5, 0.5));

            myPositionCollection8.Add(new Point3D(0.1, -0.5, -0.5));
            myPositionCollection8.Add(new Point3D(0, -1.0, 0.0));
            myPositionCollection8.Add(new Point3D(-0.1, -0.5, -0.5));
            meshSurface8.Positions = myPositionCollection8;

            //9th surface - right big wing
            Point3DCollection myPositionCollection9 = new Point3DCollection();
            myPositionCollection9.Add(new Point3D(0.5, 0.1, 0.5 + 1));
            myPositionCollection9.Add(new Point3D(4, 0, -0.5 + 1));
            myPositionCollection9.Add(new Point3D(0.5, 0.1, -0.5 + 1));
            myPositionCollection9.Add(new Point3D(0.5, -0.1, 0.5 + 1));
            myPositionCollection9.Add(new Point3D(4, 0, -0.5 + 1));
            myPositionCollection9.Add(new Point3D(0.5, -0.1, -0.5 + 1));

            myPositionCollection9.Add(new Point3D(0.5, 0.1, 0.5 + 1));
            myPositionCollection9.Add(new Point3D(4, 0, -0.5 + 1));
            myPositionCollection9.Add(new Point3D(0.5, -0.1, 0.5 + 1));

            myPositionCollection9.Add(new Point3D(0.5, 0.1, -0.5 + 1));
            myPositionCollection9.Add(new Point3D(4, 0, -0.5 + 1));
            myPositionCollection9.Add(new Point3D(0.5, -0.1, -0.5 + 1));

            meshSurface9.Positions = myPositionCollection9;

            //10th surface -left big wing
            Point3DCollection myPositionCollection10 = new Point3DCollection();
            myPositionCollection10.Add(new Point3D(-0.5, 0.1, 0.5 + 1));
            myPositionCollection10.Add(new Point3D(-4, 0, -0.5 + 1));
            myPositionCollection10.Add(new Point3D(-0.5, 0.1, -0.5 + 1));
            myPositionCollection10.Add(new Point3D(-0.5, -0.1, 0.5 + 1));
            myPositionCollection10.Add(new Point3D(-4, 0, -0.5 + 1));
            myPositionCollection10.Add(new Point3D(-0.5, -0.1, -0.5 + 1));

            myPositionCollection10.Add(new Point3D(-0.5, 0.1, 0.5 + 1));
            myPositionCollection10.Add(new Point3D(-4, 0, -0.5 + 1));
            myPositionCollection10.Add(new Point3D(-0.5, -0.1, 0.5 + 1));

            myPositionCollection10.Add(new Point3D(-0.5, 0.1, -0.5 + 1));
            myPositionCollection10.Add(new Point3D(-4, 0, -0.5 + 1));
            myPositionCollection10.Add(new Point3D(-0.5, -0.1, -0.5 + 1));

            meshSurface10.Positions = myPositionCollection10;

            //11th surface - right small wing
            Point3DCollection myPositionCollection11 = new Point3DCollection();
            myPositionCollection11.Add(new Point3D(0.5, 0.1, 0.5 - 2));
            myPositionCollection11.Add(new Point3D(2, 0, -0.5 - 2));
            myPositionCollection11.Add(new Point3D(0.5, 0.1, -0.5 - 2));
            myPositionCollection11.Add(new Point3D(0.5, -0.1, 0.5 - 2));
            myPositionCollection11.Add(new Point3D(2, 0, -0.5 - 2));
            myPositionCollection11.Add(new Point3D(0.5, -0.1, -0.5 - 2));

            myPositionCollection11.Add(new Point3D(0.5, 0.1, 0.5 - 2));
            myPositionCollection11.Add(new Point3D(2, 0, -0.5 - 2));
            myPositionCollection11.Add(new Point3D(0.5, -0.1, 0.5 - 2));

            myPositionCollection11.Add(new Point3D(0.5, 0.1, -0.5 - 2));
            myPositionCollection11.Add(new Point3D(2, 0, -0.5 - 2));
            myPositionCollection11.Add(new Point3D(0.5, -0.1, -0.5 - 2));

            meshSurface11.Positions = myPositionCollection11;

            //12th surface -left small wing
            Point3DCollection myPositionCollection12 = new Point3DCollection();
            myPositionCollection12.Add(new Point3D(-0.5, 0.1, 0.5 - 2));
            myPositionCollection12.Add(new Point3D(-2, 0, -0.5 - 2));
            myPositionCollection12.Add(new Point3D(-0.5, 0.1, -0.5 - 2));
            myPositionCollection12.Add(new Point3D(-0.5, -0.1, 0.5 - 2));
            myPositionCollection12.Add(new Point3D(-2, 0, -0.5 - 2));
            myPositionCollection12.Add(new Point3D(-0.5, -0.1, -0.5 - 2));

            myPositionCollection12.Add(new Point3D(-0.5, 0.1, 0.5 - 2));
            myPositionCollection12.Add(new Point3D(-2, 0, -0.5 - 2));
            myPositionCollection12.Add(new Point3D(-0.5, -0.1, 0.5 - 2));

            myPositionCollection12.Add(new Point3D(-0.5, 0.1, -0.5 - 2));
            myPositionCollection12.Add(new Point3D(-2, 0, -0.5 - 2));
            myPositionCollection12.Add(new Point3D(-0.5, -0.1, -0.5 - 2));

            meshSurface12.Positions = myPositionCollection12;

            //////////////////////////////////////////////////////////////////////

            // Create a collection of triangle indices for the MeshGeometry3D. For a triangle in a given 3-D mesh, the order in which the triangle's vertex positions are specified determines whether the triangle face is a front or back face.
            //7th surface
            Int32Collection myTriangleIndicesCollection7 = new Int32Collection();
            myTriangleIndicesCollection7.Add(2);
            myTriangleIndicesCollection7.Add(1);
            myTriangleIndicesCollection7.Add(0);
            myTriangleIndicesCollection7.Add(3);
            myTriangleIndicesCollection7.Add(4);
            myTriangleIndicesCollection7.Add(5);

            myTriangleIndicesCollection7.Add(6);
            myTriangleIndicesCollection7.Add(7);
            myTriangleIndicesCollection7.Add(8);

            myTriangleIndicesCollection7.Add(11);
            myTriangleIndicesCollection7.Add(10);
            myTriangleIndicesCollection7.Add(9);

            meshSurface7.TriangleIndices = myTriangleIndicesCollection7;
            //Apply the mesh to the geometry model.
            surface7.Geometry = meshSurface7;

            //8th surface
            Int32Collection myTriangleIndicesCollection8 = new Int32Collection();
            myTriangleIndicesCollection8.Add(0);
            myTriangleIndicesCollection8.Add(1);
            myTriangleIndicesCollection8.Add(2);
            myTriangleIndicesCollection8.Add(5);
            myTriangleIndicesCollection8.Add(4);
            myTriangleIndicesCollection8.Add(3);

            myTriangleIndicesCollection8.Add(8);
            myTriangleIndicesCollection8.Add(7);
            myTriangleIndicesCollection8.Add(6);

            myTriangleIndicesCollection8.Add(9);
            myTriangleIndicesCollection8.Add(10);
            myTriangleIndicesCollection8.Add(11);
            meshSurface8.TriangleIndices = myTriangleIndicesCollection8;
            // Apply the mesh to the geometry model.
            surface8.Geometry = meshSurface8;

            //9th surface
            Int32Collection myTriangleIndicesCollection9 = new Int32Collection();
            myTriangleIndicesCollection9.Add(0);
            myTriangleIndicesCollection9.Add(1);
            myTriangleIndicesCollection9.Add(2);
            myTriangleIndicesCollection9.Add(5);
            myTriangleIndicesCollection9.Add(4);
            myTriangleIndicesCollection9.Add(3);

            myTriangleIndicesCollection9.Add(8);
            myTriangleIndicesCollection9.Add(7);
            myTriangleIndicesCollection9.Add(6);

            myTriangleIndicesCollection9.Add(9);
            myTriangleIndicesCollection9.Add(10);
            myTriangleIndicesCollection9.Add(11);
            meshSurface9.TriangleIndices = myTriangleIndicesCollection9;
            // Apply the mesh to the geometry model.
            surface9.Geometry = meshSurface9;

            //10th surface
            Int32Collection myTriangleIndicesCollection10 = new Int32Collection();
            myTriangleIndicesCollection10.Add(2);
            myTriangleIndicesCollection10.Add(1);
            myTriangleIndicesCollection10.Add(0);
            myTriangleIndicesCollection10.Add(3);
            myTriangleIndicesCollection10.Add(4);
            myTriangleIndicesCollection10.Add(5);

            myTriangleIndicesCollection10.Add(6);
            myTriangleIndicesCollection10.Add(7);
            myTriangleIndicesCollection10.Add(8);

            myTriangleIndicesCollection10.Add(11);
            myTriangleIndicesCollection10.Add(10);
            myTriangleIndicesCollection10.Add(9);
            meshSurface10.TriangleIndices = myTriangleIndicesCollection10;
            // Apply the mesh to the geometry model.
            surface10.Geometry = meshSurface10;

            //11th surface
            Int32Collection myTriangleIndicesCollection11 = new Int32Collection();
            myTriangleIndicesCollection11.Add(0);
            myTriangleIndicesCollection11.Add(1);
            myTriangleIndicesCollection11.Add(2);
            myTriangleIndicesCollection11.Add(5);
            myTriangleIndicesCollection11.Add(4);
            myTriangleIndicesCollection11.Add(3);

            myTriangleIndicesCollection11.Add(8);
            myTriangleIndicesCollection11.Add(7);
            myTriangleIndicesCollection11.Add(6);

            myTriangleIndicesCollection11.Add(9);
            myTriangleIndicesCollection11.Add(10);
            myTriangleIndicesCollection11.Add(11);
            meshSurface11.TriangleIndices = myTriangleIndicesCollection11;
            // Apply the mesh to the geometry model.
            surface11.Geometry = meshSurface11;

            //12th surface
            Int32Collection myTriangleIndicesCollection12 = new Int32Collection();
            myTriangleIndicesCollection12.Add(2);
            myTriangleIndicesCollection12.Add(1);
            myTriangleIndicesCollection12.Add(0);
            myTriangleIndicesCollection12.Add(3);
            myTriangleIndicesCollection12.Add(4);
            myTriangleIndicesCollection12.Add(5);

            myTriangleIndicesCollection12.Add(6);
            myTriangleIndicesCollection12.Add(7);
            myTriangleIndicesCollection12.Add(8);

            myTriangleIndicesCollection12.Add(11);
            myTriangleIndicesCollection12.Add(10);
            myTriangleIndicesCollection12.Add(9);
            meshSurface12.TriangleIndices = myTriangleIndicesCollection12;
            // Apply the mesh to the geometry model.
            surface12.Geometry = meshSurface12;

            ////////////////////////////////////////////////////////////////////////////////////
            
            // The material specifies the material applied to the 3D object.
            // Define material and apply to the mesh geometries.
            DiffuseMaterial myMaterial7 = new DiffuseMaterial(Brushes.Goldenrod);
            surface7.Material = myMaterial7;

            DiffuseMaterial myMaterial8 = new DiffuseMaterial(new SolidColorBrush(Colors.Blue));
            surface8.Material = myMaterial8;

            DiffuseMaterial myMaterial9 = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));
            surface9.Material = myMaterial9;

            DiffuseMaterial myMaterial10 = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));
            surface10.Material = myMaterial10;

            DiffuseMaterial myMaterial11 = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));
            surface11.Material = myMaterial11;

            DiffuseMaterial myMaterial12 = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));
            surface12.Material = myMaterial12;
            #endregion

            ///////////////
            /////Nose/////
            //////////////
            #region Nose
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Repeats the same steps that were used to create the wings but in this case different points are used in order to create the nose.///
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            GeometryModel3D SurfaceNose = new GeometryModel3D();
            MeshGeometry3D meshSurfaceNose = new MeshGeometry3D();

            //Nose surfaces
            Point3DCollection myPositionCollectionNose = new Point3DCollection();
            myPositionCollectionNose.Add(new Point3D(-0.5, 0.5, 3));
            myPositionCollectionNose.Add(new Point3D(0.5, 0.5, 3));
            myPositionCollectionNose.Add(new Point3D(0, 0, 4));

            myPositionCollectionNose.Add(new Point3D(0.5, -0.5, 3));
            myPositionCollectionNose.Add(new Point3D(-0.5, -0.5, 3));
            myPositionCollectionNose.Add(new Point3D(0, 0, 4));

            myPositionCollectionNose.Add(new Point3D(-0.5, 0.5, 3));
            myPositionCollectionNose.Add(new Point3D(-0.5, -0.5, 3));
            myPositionCollectionNose.Add(new Point3D(0, 0, 4));

            myPositionCollectionNose.Add(new Point3D(0.5, 0.5, 3));
            myPositionCollectionNose.Add(new Point3D(0.5, -0.5, 3));
            myPositionCollectionNose.Add(new Point3D(0, 0, 4));


            Int32Collection myTriangleIndicesCollectionNose = new Int32Collection();
            myTriangleIndicesCollectionNose.Add(2);
            myTriangleIndicesCollectionNose.Add(1);
            myTriangleIndicesCollectionNose.Add(0);

            myTriangleIndicesCollectionNose.Add(5);
            myTriangleIndicesCollectionNose.Add(4);
            myTriangleIndicesCollectionNose.Add(3);

            myTriangleIndicesCollectionNose.Add(6);
            myTriangleIndicesCollectionNose.Add(7);
            myTriangleIndicesCollectionNose.Add(8);

            myTriangleIndicesCollectionNose.Add(11);
            myTriangleIndicesCollectionNose.Add(10);
            myTriangleIndicesCollectionNose.Add(9);

            meshSurfaceNose.Positions = myPositionCollectionNose;
            meshSurfaceNose.TriangleIndices = myTriangleIndicesCollectionNose;
            SurfaceNose.Geometry = meshSurfaceNose;
            DiffuseMaterial myMaterialNose = new DiffuseMaterial(Brushes.DarkCyan);
            SurfaceNose.Material = myMaterialNose;
            #endregion

            //Apply the rotation to the models.
            surface7.Transform = myRotateTransform3D;
            surface8.Transform = myRotateTransform3D;
            surface9.Transform = myRotateTransform3D;
            surface10.Transform = myRotateTransform3D;
            surface11.Transform = myRotateTransform3D;
            surface12.Transform = myRotateTransform3D;
            SurfaceNose.Transform = myRotateTransform3D;

            // Add the geometry model to the model group.
            myModel3DGroup.Children.Add(surface7);
            myModel3DGroup.Children.Add(surface8);
            myModel3DGroup.Children.Add(surface9);
            myModel3DGroup.Children.Add(surface10);
            myModel3DGroup.Children.Add(surface11);
            myModel3DGroup.Children.Add(surface12);
            myModel3DGroup.Children.Add(SurfaceNose);

            // Add the group of models to the ModelVisual3d. The aircraft and axes are on seperate groups.
            myModelVisual3D.Content = myModel3DGroup;
            myViewport3D.Children.Add(myModelVisual3D);

            myModelVisual3DAxes.Content = myModel3DGroupAxes;
            myViewport3D.Children.Add(myModelVisual3DAxes);

            //Place the Viewport on a specified section on the grid.
            Grid.SetRow(myViewport3D, 0);
            Grid.SetColumn(myViewport3D, 1);
           
            myGrid.Children.Add(myViewport3D);

        }

        //Draws a cuboid from point 1 to point 2 given the model group and orientation.
        public void DrawCuboid(Model3DGroup the3DGroup, QuaternionRotation3D myRotQuat3D, Point3D point1, Point3D point2, Brush surfaceColour, float thickness)
        {
            float scalingFactor = (float)Math.Sqrt(thickness * thickness / 2);  //width and height of the cuboid
            Vector3D vAxis = point2 - point1;                                   // vector Axis
            Point3D p1;                                                         //point 1 of the first square
            Point3D p2;                                                         //point 2 of the first square
            Point3D p3;                                                         //point 3 of the first square
            Point3D p4;                                                         //point4 of the first square
            Point3D p5;                                                         //point 1 of the second square
            Point3D p6;                                                         //point 2 of the second square
            Point3D p7;                                                         //point 3 of the second square
            Point3D p8;                                                         //point 4 of the second square
            Point3D Origin = new Point3D(0, 0, 0);

            Vector3D ConnectionLine1;                                           //vector on the same plane of vAxis at point1
            Vector3D ConnectionLine2;                                           //perpendicular vector on the same plane of vAxis at point1
            Vector3D ConnectionLine3;                                           //vector on the same plane of vAxis at point2
            Vector3D ConnectionLine4;                                           //perpendicular vector on the same plane of vAxis at point2

            //creates 2 parallel squares based on the two given points
            //calculating the first 4 points for the first square
            #region 8 Square Points
            if (vAxis.X != 0)
            {
                ConnectionLine1.Y = 1;
                ConnectionLine1.Z = 1;
                ConnectionLine1.X = ((-vAxis.Y * (p1.Y - point1.Y) - vAxis.Z * (p1.Z - point1.Z)) / vAxis.X) + point1.X;
            }
            else if (vAxis.Y != 0)
            {
                ConnectionLine1.X = 1;
                ConnectionLine1.Z = 1;
                ConnectionLine1.Y = ((-vAxis.X * (p1.X - point1.X) - vAxis.Z * (p1.Z - point1.Z)) / vAxis.Y) + point1.Y;
            }
            else if (vAxis.Z != 0)
            {
                ConnectionLine1.X = 1;
                ConnectionLine1.Y = 1;
                ConnectionLine1.Z = ((-vAxis.X * (p1.X - point1.X) - vAxis.Y * (p1.Y - point1.Y)) / vAxis.Z) + point1.Z;
            }

            ConnectionLine1 = ConnectionLine1 - (point1 - Origin);
            ConnectionLine1.Normalize();
            
            p1 = ConnectionLine1 * scalingFactor + point1;                   //point 1 is arbitary but on the same plane of vAxis at point 1
            p2 = -ConnectionLine1 * scalingFactor + point1;                  //point 2 is the opposite point of point 1


            ConnectionLine2 = Vector3D.CrossProduct(vAxis, ConnectionLine1); //caluclating a vector 90 degrees from the point 1
            ConnectionLine2.Normalize();
            p3 = ConnectionLine2 * scalingFactor + point1;                   //point 3 is 90 degrees from point 1
            p4 = -ConnectionLine2 * scalingFactor + point1;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Repeat the first step but for point 2
            if (vAxis.X != 0)
            {
                ConnectionLine3.Y = 1;
                ConnectionLine3.Z = 1;
                ConnectionLine3.X = ((-vAxis.Y * (p5.Y - point2.Y) - vAxis.Z * (p5.Z - point2.Z)) / vAxis.X) + point2.X;
            }
            else if (vAxis.Y != 0)
            {
                ConnectionLine3.X = 1;
                ConnectionLine3.Z = 1;
                ConnectionLine3.Y = ((-vAxis.X * (p5.X - point2.X) - vAxis.Z * (p5.Z - point2.Z)) / vAxis.Y) + point2.Y;
            }
            else if (vAxis.Z != 0)
            {
                ConnectionLine3.X = 1;
                ConnectionLine3.Y = 1;
                ConnectionLine3.Z = ((-vAxis.X * (p5.X - point2.X) - vAxis.Y * (p5.Y - point2.Y)) / vAxis.Z) + point2.Z;
            }

            ConnectionLine3 = ConnectionLine3 - (point2 - Origin);              //point 1 is arbitary but on the same plane of vAxis at point 1
            ConnectionLine3.Normalize();                                        //point 2 is the opposite point of point 1
            p5 = (ConnectionLine3 * scalingFactor + point2);

            p6 = (-ConnectionLine3 * scalingFactor + point2);                   //caluclating a vector 90 degrees from the point 1

            ConnectionLine4 = Vector3D.CrossProduct(vAxis, ConnectionLine3);    //point 3 is 90 degrees from point 1
            ConnectionLine4.Normalize();
            p7 = ConnectionLine4 * scalingFactor + point2;
            p8 = -ConnectionLine4 * scalingFactor + point2;
            #endregion

            //create and mesh surfaces the six faces of cuboid
            GeometryModel3D surface_1 = new GeometryModel3D();
            MeshGeometry3D Meshsurface_1 = new MeshGeometry3D();

            GeometryModel3D surface_2 = new GeometryModel3D();
            MeshGeometry3D Meshsurface_2 = new MeshGeometry3D();

            GeometryModel3D surface_3 = new GeometryModel3D();
            MeshGeometry3D Meshsurface_3 = new MeshGeometry3D();

            GeometryModel3D surface_4 = new GeometryModel3D();
            MeshGeometry3D Meshsurface_4 = new MeshGeometry3D();

            GeometryModel3D surface_5 = new GeometryModel3D();
            MeshGeometry3D Meshsurface_5 = new MeshGeometry3D();

            GeometryModel3D surface_6 = new GeometryModel3D();
            MeshGeometry3D Meshsurface_6 = new MeshGeometry3D();

            RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            myRotateTransform3D.Rotation = myRotQuat3D;

            Point3DCollection myPositionCollection_1 = new Point3DCollection();
            myPositionCollection_1.Add(p1);
            myPositionCollection_1.Add(p2);
            myPositionCollection_1.Add(p3);
            myPositionCollection_1.Add(p1);
            myPositionCollection_1.Add(p4);
            myPositionCollection_1.Add(p2);
            Meshsurface_1.Positions = myPositionCollection_1;


            Int32Collection myTriangleIndicesCollection_1 = new Int32Collection();
            myTriangleIndicesCollection_1.Add(0);
            myTriangleIndicesCollection_1.Add(1);
            myTriangleIndicesCollection_1.Add(2);
            myTriangleIndicesCollection_1.Add(3);
            myTriangleIndicesCollection_1.Add(4);
            myTriangleIndicesCollection_1.Add(5);
            Meshsurface_1.TriangleIndices = myTriangleIndicesCollection_1;
            // Apply the mesh to the geometry model.
            surface_1.Geometry = Meshsurface_1;

            DiffuseMaterial myMaterial_1 = new DiffuseMaterial(surfaceColour);
            surface_1.Material = myMaterial_1;

            surface_1.Transform = myRotateTransform3D;

            the3DGroup.Children.Add(surface_1);

            /////////////////////////////////////////////////////////////////////////////

            Point3DCollection myPositionCollection_2 = new Point3DCollection();
            myPositionCollection_2.Add(p5);
            myPositionCollection_2.Add(p6);
            myPositionCollection_2.Add(p7);
            myPositionCollection_2.Add(p5);
            myPositionCollection_2.Add(p8);
            myPositionCollection_2.Add(p6);
            Meshsurface_2.Positions = myPositionCollection_2;

            Int32Collection myTriangleIndicesCollection_2 = new Int32Collection();
            myTriangleIndicesCollection_2.Add(5);
            myTriangleIndicesCollection_2.Add(4);
            myTriangleIndicesCollection_2.Add(3);
            myTriangleIndicesCollection_2.Add(2);
            myTriangleIndicesCollection_2.Add(1);
            myTriangleIndicesCollection_2.Add(0);
            Meshsurface_2.TriangleIndices = myTriangleIndicesCollection_2;
            // Apply the mesh to the geometry model.
            surface_2.Geometry = Meshsurface_2;

            DiffuseMaterial myMaterial_2 = new DiffuseMaterial(surfaceColour);
            surface_2.Material = myMaterial_2;

            surface_2.Transform = myRotateTransform3D;

            the3DGroup.Children.Add(surface_2);

            /////////////////////////////////////////////////////////////////////////////

            Point3DCollection myPositionCollection_3 = new Point3DCollection();
            myPositionCollection_3.Add(p1);
            myPositionCollection_3.Add(p7);
            myPositionCollection_3.Add(p3);
            myPositionCollection_3.Add(p1);
            myPositionCollection_3.Add(p5);
            myPositionCollection_3.Add(p7);
            Meshsurface_3.Positions = myPositionCollection_3;

            Int32Collection myTriangleIndicesCollection_3 = new Int32Collection();
            myTriangleIndicesCollection_3.Add(5);
            myTriangleIndicesCollection_3.Add(4);
            myTriangleIndicesCollection_3.Add(3);
            myTriangleIndicesCollection_3.Add(2);
            myTriangleIndicesCollection_3.Add(1);
            myTriangleIndicesCollection_3.Add(0);
            Meshsurface_3.TriangleIndices = myTriangleIndicesCollection_3;
            // Apply the mesh to the geometry model.
            surface_3.Geometry = Meshsurface_3;

            DiffuseMaterial myMaterial_3 = new DiffuseMaterial(surfaceColour);
            surface_3.Material = myMaterial_3;

            surface_3.Transform = myRotateTransform3D;

            the3DGroup.Children.Add(surface_3);

            /////////////////////////////////////////////////////////////////////////////

            Point3DCollection myPositionCollection_4 = new Point3DCollection();
            myPositionCollection_4.Add(p8);
            myPositionCollection_4.Add(p2);
            myPositionCollection_4.Add(p6);
            myPositionCollection_4.Add(p8);
            myPositionCollection_4.Add(p4);
            myPositionCollection_4.Add(p2);
            Meshsurface_4.Positions = myPositionCollection_4;

            Int32Collection myTriangleIndicesCollection_4 = new Int32Collection();
            myTriangleIndicesCollection_4.Add(5);
            myTriangleIndicesCollection_4.Add(4);
            myTriangleIndicesCollection_4.Add(3);
            myTriangleIndicesCollection_4.Add(2);
            myTriangleIndicesCollection_4.Add(1);
            myTriangleIndicesCollection_4.Add(0);
            Meshsurface_4.TriangleIndices = myTriangleIndicesCollection_4;
            // Apply the mesh to the geometry model.
            surface_4.Geometry = Meshsurface_4;

            DiffuseMaterial myMaterial_4 = new DiffuseMaterial(surfaceColour);
            surface_4.Material = myMaterial_4;

            surface_4.Transform = myRotateTransform3D;

            the3DGroup.Children.Add(surface_4);

            /////////////////////////////////////////////////////////////////////////////

            Point3DCollection myPositionCollection_5 = new Point3DCollection();
            myPositionCollection_5.Add(p7);
            myPositionCollection_5.Add(p3);
            myPositionCollection_5.Add(p2);
            myPositionCollection_5.Add(p2);
            myPositionCollection_5.Add(p6);
            myPositionCollection_5.Add(p7);
            Meshsurface_5.Positions = myPositionCollection_5;

            Int32Collection myTriangleIndicesCollection_5 = new Int32Collection();
            myTriangleIndicesCollection_5.Add(0);
            myTriangleIndicesCollection_5.Add(1);
            myTriangleIndicesCollection_5.Add(2);
            myTriangleIndicesCollection_5.Add(3);
            myTriangleIndicesCollection_5.Add(4);
            myTriangleIndicesCollection_5.Add(5);
            Meshsurface_5.TriangleIndices = myTriangleIndicesCollection_5;
            // Apply the mesh to the geometry model.
            surface_5.Geometry = Meshsurface_5;

            DiffuseMaterial myMaterial_5 = new DiffuseMaterial(surfaceColour);
            surface_5.Material = myMaterial_5;

            surface_5.Transform = myRotateTransform3D;

            the3DGroup.Children.Add(surface_5);

            /////////////////////////////////////////////////////////////////////////////

            Point3DCollection myPositionCollection_6 = new Point3DCollection();
            myPositionCollection_6.Add(p5);
            myPositionCollection_6.Add(p8);
            myPositionCollection_6.Add(p1);
            myPositionCollection_6.Add(p8);
            myPositionCollection_6.Add(p4);
            myPositionCollection_6.Add(p1);
            Meshsurface_6.Positions = myPositionCollection_6;

            Int32Collection myTriangleIndicesCollection_6 = new Int32Collection();
            myTriangleIndicesCollection_6.Add(0);
            myTriangleIndicesCollection_6.Add(1);
            myTriangleIndicesCollection_6.Add(2);
            myTriangleIndicesCollection_6.Add(3);
            myTriangleIndicesCollection_6.Add(4);
            myTriangleIndicesCollection_6.Add(5);
            Meshsurface_6.TriangleIndices = myTriangleIndicesCollection_6;
            // Apply the mesh to the geometry model.
            surface_6.Geometry = Meshsurface_6;

            DiffuseMaterial myMaterial_6 = new DiffuseMaterial(surfaceColour);
            surface_6.Material = myMaterial_6;

            surface_6.Transform = myRotateTransform3D;

            the3DGroup.Children.Add(surface_6);
        }

        //Opens the communication between the Arduino and the Application (Button Control on the GUI)
        private void OpenCOM(object sender, RoutedEventArgs e)
        {
            int _BaudRate; //Stores the converted integer baud rate that was specified on the combo box

            //checks whether the communication is possible. (i.e. The Arduino is connected to the App)
            try
            {
                //Opens the port if not open
                if (!myPort.IsOpen)
                {
                    Int32.TryParse(cbxBaudRate.Text, out _BaudRate);
                    myPort.BaudRate = _BaudRate;
                    myPort.PortName = txtPortName.Text;
                    myPort.Parity = Parity.None;
                    myPort.DataBits = 8;
                    myPort.StopBits = StopBits.One;

                    
                    myPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    myPort.Open();
                    myPort.NewLine = "\n";
                 
                    OpenComms.Content = "Disconnect";
                }
                //Closes the port
                else
                {
                    myPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                    
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        myPort.DtrEnable = false;
                        myPort.RtsEnable = false;
                        myPort.Close();
                    }), DispatcherPriority.ContextIdle);

                   OpenComms.Content = "Connect";
                }
            }
            catch (Exception ex)
            {
                //shows the error
                MessageBox.Show("Please check your connection! -> " + ex);
            }

            //maps a number to the selected filter
            if (cbxFilter.Text == "Kalman Filter")
            {
                _selectedFilter = 1;
            }
            else if (cbxFilter.Text == "Kalman Filter with Bias")
            {
                _selectedFilter = 2;
            }
            else if (cbxFilter.Text == "Complementary Filter")
            {
                _selectedFilter = 3;
            }

        }

        //To receive data from serial port, we need to create an EventHandler for the "SerialDataReceivedEventHandler"
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string[] quatStringArray = new string[7];       //array that holds the receiving elemets as strings
            float[] quatFloatArrayTemp = new float[6];      //array that holds the converted numbers
            float[] sensorFloatArray = new float[6];        //puts the sensor data in array for background thread
            bool isNumeric;                                 //the data type is number
            bool is6Elemets;                                //the array has 6 numbers
            bool arrayHasNaN = false;                       //the array has an infinity/NaN
            string line = null;                             //Data coming as a line from the serial port

            try
            {
                line = sp.ReadLine();
            }
            catch (Exception) { }
        
            //checks the receiveing line is not null
            if (line != null)
            {
                quatStringArray = line.Split(" ");
            }

            //checks that they array has 7 elemets
            if (quatStringArray.Length == 7)
            {
                is6Elemets = true;
            }
            else
            {
                is6Elemets = false;
            }

            //converts the array from string to float and checks those numbers for infinity or NaN
            for (int index = 1; index < quatStringArray.Length; index++)
            {
                isNumeric = float.TryParse(quatStringArray[index], out _);
                if (isNumeric && is6Elemets)
                {
                    quatFloatArrayTemp[index - 1] = Convert.ToSingle(quatStringArray[index]);

                    if (double.IsNaN(quatFloatArrayTemp[index - 1]) || double.IsInfinity(quatFloatArrayTemp[index - 1]))
                    {
                        arrayHasNaN = true;
                        break;
                    }
                }
            }
        
            //if the elemets are NOT NaN or Infinity then pass those values as inputs to the background thread that runs the Filter Algorithm
            if (arrayHasNaN == false && is6Elemets && !bgWorker.IsBusy)
            {               
                sensorFloatArray[0] = quatFloatArrayTemp[0];
                sensorFloatArray[1] = quatFloatArrayTemp[1];
                sensorFloatArray[2] = quatFloatArrayTemp[2];
                sensorFloatArray[3] = quatFloatArrayTemp[3];
                sensorFloatArray[4] = quatFloatArrayTemp[4];
                sensorFloatArray[5] = quatFloatArrayTemp[5];

                bgWorker.RunWorkerAsync(argument: sensorFloatArray);
            }
        }

        //start the operation that performs the potentially time-consuming work
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            float[] dataArray = (float[])e.Argument;    //Put the data of the sensors to an array

            if (_selectedFilter == 1)
            {
                e.Result = Filters.KalmanFilter(dataArray[0], dataArray[1], dataArray[2], dataArray[3], dataArray[4], dataArray[5]);
            }
            else if (_selectedFilter == 2)
            {
                e.Result = Filters.KalmanFilterBias(dataArray[0], dataArray[1], dataArray[2], dataArray[3], dataArray[4], dataArray[5]);
            }
            else if (_selectedFilter == 3)
            {
                e.Result = Filters.ComplementaryFilter(dataArray[0], dataArray[1], dataArray[2], dataArray[3], dataArray[4], dataArray[5]);
            }

        }

        //when the background thread completes its work, the results (quaternions) are used to update the text boxes and orientation of the plane on the GUI
        public void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            float[] KalmanQuat = (float[])e.Result;

            if (e.Error != null)
            {
                txtQw.Text = Convert.ToString("Error");
                txtQx.Text = Convert.ToString("Error");
                txtQy.Text = Convert.ToString("Error");
                txtQz.Text = Convert.ToString("Error");
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    txtQw.Text = Convert.ToString(KalmanQuat[0]);
                    txtQx.Text = Convert.ToString(KalmanQuat[1]);
                    txtQy.Text = Convert.ToString(KalmanQuat[2]);
                    txtQz.Text = Convert.ToString(KalmanQuat[3]);
                }), DispatcherPriority.ContextIdle);
                Dispatcher.BeginInvoke(new LineReceivedEvent(LineReceived), KalmanQuat);

            }
        }

        //The delegate is used to write to the UI thread from a non-UI thread.
        private delegate void LineReceivedEvent(float[] quatArray);

        //function that tranforms the models using quaternions
        private void LineReceived(float[] quatArray)
        {
            mySerialPortQuaternion.W = quatArray[0];
            mySerialPortQuaternion.X = quatArray[1];
            mySerialPortQuaternion.Y = quatArray[2];
            mySerialPortQuaternion.Z = quatArray[3];
            QuaternionRotation3D myDroneRotation3D = new QuaternionRotation3D(mySerialPortQuaternion);
            myDroneTransform3D.Rotation = myDroneRotation3D;

            myModel3DGroup.Transform = myDroneTransform3D;
        }

    }
}
