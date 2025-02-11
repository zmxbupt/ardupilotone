﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

using System.Threading;
 
using System.Drawing.Drawing2D;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;


// Control written by Michael Oborne 2011
// dual opengl and GDI+

namespace hud
{
    public class HUD : GLControl
    {
        object paintlock = new object();
        object streamlock = new object();
        MemoryStream _streamjpg = new MemoryStream();
        public MemoryStream streamjpg { get { lock (streamlock) { return _streamjpg; } } set { lock (streamlock) { _streamjpg = value; } } }
        /// <summary>
        /// this is to reduce cpu usage
        /// </summary>
        public bool streamjpgenable = false;

        Bitmap[] charbitmaps = new Bitmap[6000];
        int[] charbitmaptexid = new int[6000];
        int[] charwidth = new int[6000];

        public int huddrawtime = 0;

        public bool opengl { get { return base.UseOpenGL; } set { base.UseOpenGL = value; } }

        public HUD()
        {
            if (this.DesignMode)
                return;

            InitializeComponent();

            graphicsObject = this;
            graphicsObjectGDIP = Graphics.FromImage(objBitmap);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HUD));
            this.SuspendLayout();
            // 
            // HUD
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.Name = "HUD";
            resources.ApplyResources(this, "$this");
            this.ResumeLayout(false);

        }

        float _roll;
        float _navroll;
        float _pitch;
        float _navpitch;
        float _heading;
        float _targetheading;
        float _alt;
        float _targetalt;
        float _groundspeed;
        float _airspeed;
        float _targetspeed;
        float _batterylevel;
        float _batteryremaining;
        float _gpsfix;
        float _gpshdop;
        float _disttowp;
        float _groundcourse;
        float _xtrack_error;
        float _turnrate;
        float _verticalspeed;
        string _mode = "Manual";
        int _wpno;

        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float roll { get { return _roll; } set { if (_roll != value) { _roll = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float navroll { get { return _navroll; } set { if (_navroll != value) { _navroll = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float pitch { get { return _pitch; } set { if (_pitch != value) { _pitch = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float navpitch { get { return _navpitch; } set { if (_navpitch != value) { _navpitch = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float heading { get { return _heading; } set { if (_heading != value) { _heading = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float targetheading { get { return _targetheading; } set { if (_targetheading != value) { _targetheading = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float alt { get { return _alt; } set { if (_alt != value) { _alt = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float targetalt { get { return _targetalt; } set { if (_targetalt != value) { _targetalt = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float groundspeed { get { return _groundspeed; } set { if (_groundspeed != value) { _groundspeed = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float airspeed { get { return _airspeed; } set { if (_airspeed != value) { _airspeed = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float targetspeed { get { return _targetspeed; } set { if (_targetspeed != value) { _targetspeed = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float batterylevel { get { return _batterylevel; } set { if (_batterylevel != value) { _batterylevel = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float batteryremaining { get { return _batteryremaining; } set { if (_batteryremaining != value) { _batteryremaining = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float gpsfix { get { return _gpsfix; } set { if (_gpsfix != value) { _gpsfix = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float gpshdop { get { return _gpshdop; } set { if (_gpshdop != value) { _gpshdop = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float disttowp { get { return _disttowp; } set { if (_disttowp != value) { _disttowp = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public string mode { get { return _mode; } set { if (_mode != value) { _mode = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public int wpno { get { return _wpno; } set { if (_wpno != value) { _wpno = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float groundcourse { get { return _groundcourse; } set { if (_groundcourse != value) { _groundcourse = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float xtrack_error { get { return _xtrack_error; } set { if (_xtrack_error != value) { _xtrack_error = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float turnrate { get { return _turnrate; } set { if (_turnrate != value) { _turnrate = value; this.Invalidate(); } } }
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public float verticalspeed { get { return _verticalspeed; } set { if (_verticalspeed != value) { _verticalspeed = value; this.Invalidate(); } } }

        public bool bgon = true;
        public bool hudon = true;

        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Values")]
        public Color hudcolor { get { return whitePen.Color; } set { whitePen = new Pen(value, 2); } }

        Pen whitePen = new Pen(Color.White, 2);

        public Image bgimage { set { _bgimage = value; this.Invalidate(); } }
        Image _bgimage;

        // move these global as they rarely change - reduce GC
        Font font = new Font("Arial", 10);
        Bitmap objBitmap = new Bitmap(1024, 1024);
        int count = 0;
        DateTime countdate = DateTime.Now;
        HUD graphicsObject;
        Graphics graphicsObjectGDIP;

        DateTime starttime = DateTime.MinValue;

        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HUD));

        public override void Refresh()
        {
            //base.Refresh();
            OnPaint(new PaintEventArgs(this.CreateGraphics(),this.ClientRectangle));
        }

        protected override void OnLoad(EventArgs e)
        {
            if (this.DesignMode) 
                return;

            if (opengl)
            {
                try
                {

                    GraphicsMode test = this.GraphicsMode;
                    Console.WriteLine(test.ToString());
                    Console.WriteLine("Vendor: " + GL.GetString(StringName.Vendor));
                    Console.WriteLine("Version: " + GL.GetString(StringName.Version));
                    Console.WriteLine("Device: " + GL.GetString(StringName.Renderer));
                    //Console.WriteLine("Extensions: " + GL.GetString(StringName.Extensions));

                    int[] viewPort = new int[4];

                    GL.GetInteger(GetPName.Viewport, viewPort);

                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    GL.Ortho(0, Width, Height, 0, -1, 1);
                    GL.MatrixMode(MatrixMode.Modelview);
                    GL.LoadIdentity();

                    GL.PushAttrib(AttribMask.DepthBufferBit);
                    GL.Disable(EnableCap.DepthTest);
                    //GL.Enable(EnableCap.Texture2D); 
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                    GL.Enable(EnableCap.Blend);

                }
                catch (Exception ex) { Console.WriteLine("HUD opengl onload " + ex.ToString()); }

                try
                {
                    GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

                    GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
                    GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
                    GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

                    GL.Hint(HintTarget.TextureCompressionHint, HintMode.Nicest);
                }
                catch { }

                try
                {

                    GL.Enable(EnableCap.LineSmooth);
                    GL.Enable(EnableCap.PointSmooth);
                    GL.Enable(EnableCap.PolygonSmooth);

                }
                catch { }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //GL.Enable(EnableCap.AlphaTest);

            if (this.DesignMode)
            {
                e.Graphics.Clear(this.BackColor);
                e.Graphics.Flush();
                return;
            }

            if ((DateTime.Now - starttime).TotalMilliseconds < 30 && (_bgimage == null))
            {
                //Console.WriteLine("ms "+(DateTime.Now - starttime).TotalMilliseconds);
                //e.Graphics.DrawImageUnscaled(objBitmap, 0, 0);          
                return;              
            }

            starttime = DateTime.Now;

            if (opengl)
            {
                MakeCurrent();

                GL.Clear(ClearBufferMask.ColorBufferBit);

            }

            doPaint(e);

            if (opengl) {
                this.SwapBuffers();
            }

            count++;

            huddrawtime += (int)(DateTime.Now - starttime).TotalMilliseconds;

            if (DateTime.Now.Second != countdate.Second)
            {
                countdate = DateTime.Now;
                //Console.WriteLine("HUD " + count + " hz drawtime " + (huddrawtime / count) + " gl " + opengl);
                count = 0;
                huddrawtime = 0;
            }
        }

        void Clear(Color color)
        {
            if (opengl)
            {
                GL.ClearColor(color);
            } else 
            {
                graphicsObjectGDIP.Clear(color);
            }
        }

        const float rad2deg = (float)(180 / Math.PI);
        const float deg2rad = (float)(1.0 / rad2deg);

        public void DrawArc(Pen penn,RectangleF rect, float start,float degrees)
        {
            if (opengl)
            {
                GL.LineWidth(penn.Width);
                GL.Color4(penn.Color);

                GL.Begin(BeginMode.LineStrip);
                start -= 90;
                float x = 0, y = 0;
                for (int i = (int)start; i <= start + degrees; i++)
                {
                    x = (float)Math.Sin(i * deg2rad) * rect.Width / 2;
                    y = (float)Math.Cos(i * deg2rad) * rect.Height / 2;
                    x = x + rect.X + rect.Width / 2;
                    y = y + rect.Y + rect.Height / 2;
                    GL.Vertex2(x, y);
                }
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.DrawArc(penn, rect, start, degrees);
            }
        }

        public void DrawEllipse(Pen penn, Rectangle rect)
        {
            if (opengl)
            {
                GL.LineWidth(penn.Width);
                GL.Color4(penn.Color);

                GL.Begin(BeginMode.LineLoop);
                float x, y;
                for (float i = 0; i < 360; i += 1)
                {
                    x = (float)Math.Sin(i * deg2rad) * rect.Width / 2;
                    y = (float)Math.Cos(i * deg2rad) * rect.Height / 2;
                    x = x + rect.X + rect.Width / 2;
                    y = y + rect.Y + rect.Height / 2;
                    GL.Vertex2(x, y);
                }
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.DrawEllipse(penn, rect);
            }
        }

        int texture;
        Bitmap bitmap = new Bitmap(512,512);

        public void DrawImage(Image img, int x, int y, int width, int height)
        {
            if (opengl)
            {
                if (img == null)
                    return;
                //bitmap = new Bitmap(512,512);

                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                    //draw the image into the target bitmap 
                    graphics.DrawImage(img, 0, 0, bitmap.Width, bitmap.Height);
                }


                GL.DeleteTexture(texture);

                GL.GenTextures(1, out texture);
                GL.BindTexture(TextureTarget.Texture2D, texture);

                BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                //Console.WriteLine("w {0} h {1}",data.Width, data.Height);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bitmap.UnlockBits(data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                GL.Enable(EnableCap.Texture2D);

                GL.BindTexture(TextureTarget.Texture2D, texture);

                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0, this.Height);
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(this.Width, this.Height);
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(this.Width, 0);
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0, 0);

                GL.End();

                GL.Disable(EnableCap.Texture2D);
            }
            else
            {
                graphicsObjectGDIP.DrawImage(img,x,y,width,height);
            }
        }

        public void DrawPath(Pen penn, GraphicsPath gp)
        {
            try
            {
               DrawPolygon(penn, gp.PathPoints);
            }
            catch { }
        }

        public void FillPath(Brush brushh, GraphicsPath gp)
        {
            try
            {
                FillPolygon(brushh, gp.PathPoints);
            }
            catch { }
        }

        public void SetClip(Rectangle rect)
        {
            
        }

        public void ResetClip()
        {

        }

        public void ResetTransform()
        {
            if (opengl)
            {
                GL.LoadIdentity();
            }
            else
            {
                graphicsObjectGDIP.ResetTransform();
            }
        }

        public void RotateTransform(float angle)
        {
            if (opengl)
            {
                GL.Rotate(angle, 0, 0, 1);
            }
            else
            {
                graphicsObjectGDIP.RotateTransform(angle);
            }
        }

        public void TranslateTransform(float x, float y)
        {
            if (opengl)
            {
                GL.Translate(x, y, 0f);
            }
            else
            {
                graphicsObjectGDIP.TranslateTransform(x, y);
            }
        }

        public void FillPolygon(Brush brushh, Point[] list)
        {
            if (opengl)
            {
                GL.Begin(BeginMode.TriangleFan);
                GL.Color4(((SolidBrush)brushh).Color);
                foreach (Point pnt in list)
                {
                    GL.Vertex2(pnt.X, pnt.Y);
                }
                GL.Vertex2(list[list.Length - 1].X, list[list.Length - 1].Y);
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.FillPolygon(brushh, list);
            }
        }

        public void FillPolygon(Brush brushh, PointF[] list)
        {
            if (opengl)
            {
                GL.Begin(BeginMode.Quads);
                GL.Color4(((SolidBrush)brushh).Color);
                foreach (PointF pnt in list)
                {
                    GL.Vertex2(pnt.X, pnt.Y);
                }
                GL.Vertex2(list[0].X, list[0].Y);
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.FillPolygon(brushh, list);
            }
        }

        public void DrawPolygon(Pen penn, Point[] list)
        {
            if (opengl)
            {
                GL.LineWidth(penn.Width);
                GL.Color4(penn.Color);

                GL.Begin(BeginMode.LineLoop);
                foreach (Point pnt in list)
                {
                    GL.Vertex2(pnt.X, pnt.Y);
                }
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.DrawPolygon(penn, list);
            }                       
        }

        public void DrawPolygon(Pen penn, PointF[] list)
        {         
            if (opengl)
            {
            GL.LineWidth(penn.Width);
            GL.Color4(penn.Color);

            GL.Begin(BeginMode.LineLoop);
            foreach (PointF pnt in list)
            {
                GL.Vertex2(pnt.X, pnt.Y);
            }

            GL.End();
            }
            else
            {
                graphicsObjectGDIP.DrawPolygon(penn, list);
            } 
        }


        public void FillRectangle(Brush brushh, RectangleF rectf)
        {
            if (opengl)
            {
                float x1 = rectf.X;
                float y1 = rectf.Y;

                float width = rectf.Width;
                float height = rectf.Height;

                GL.Begin(BeginMode.Quads);

                if (((Type)brushh.GetType()) == typeof(LinearGradientBrush))
                {
                    LinearGradientBrush temp = (LinearGradientBrush)brushh;
                    GL.Color4(temp.LinearColors[0]);
                }
                else
                {
                    GL.Color4(((SolidBrush)brushh).Color.R / 255f, ((SolidBrush)brushh).Color.G / 255f, ((SolidBrush)brushh).Color.B / 255f, ((SolidBrush)brushh).Color.A / 255f);
                }

                GL.Vertex2(x1, y1);
                GL.Vertex2(x1 + width, y1);

                if (((Type)brushh.GetType()) == typeof(LinearGradientBrush))
                {
                    LinearGradientBrush temp = (LinearGradientBrush)brushh;
                    GL.Color4(temp.LinearColors[1]);
                }
                else
                {
                    GL.Color4(((SolidBrush)brushh).Color.R / 255f, ((SolidBrush)brushh).Color.G / 255f, ((SolidBrush)brushh).Color.B / 255f, ((SolidBrush)brushh).Color.A / 255f);
                }

                GL.Vertex2(x1 + width, y1 + height);
                GL.Vertex2(x1, y1 + height);
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.FillRectangle(brushh, rectf);
            }
        }

        public void DrawRectangle(Pen penn, RectangleF rect)
        {
            DrawRectangle(penn, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen penn, double x1, double y1, double width, double height)
        {

            if (opengl)
            {
                GL.LineWidth(penn.Width);
                GL.Color4(penn.Color);

                GL.Begin(BeginMode.LineLoop);
                GL.Vertex2(x1, y1);
                GL.Vertex2(x1 + width, y1);
                GL.Vertex2(x1 + width, y1 + height);
                GL.Vertex2(x1, y1 + height);
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.DrawRectangle(penn, (float)x1, (float)y1, (float)width, (float)height);
            }
        }

        public void DrawLine(Pen penn, double x1, double y1, double x2, double y2)
        {

            if (opengl)
            {
                GL.Color4(penn.Color);
                GL.LineWidth(penn.Width);

                GL.Begin(BeginMode.Lines);
                GL.Vertex2(x1, y1);
                GL.Vertex2(x2, y2);
                GL.End();
            }
            else
            {
                graphicsObjectGDIP.DrawLine(penn, (float)x1, (float)y1, (float)x2, (float)y2);
            }
        }

        void doPaint(PaintEventArgs e)
        {
            try
            {
                if (graphicsObjectGDIP == null || !opengl && (objBitmap.Width != this.Width || objBitmap.Height != this.Height))
                {
                    objBitmap = new Bitmap(this.Width, this.Height);
                    graphicsObjectGDIP = Graphics.FromImage(objBitmap);

                    graphicsObjectGDIP.SmoothingMode = SmoothingMode.AntiAlias;
                    graphicsObjectGDIP.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphicsObjectGDIP.CompositingMode = CompositingMode.SourceOver;
                    graphicsObjectGDIP.CompositingQuality = CompositingQuality.HighSpeed;
                    graphicsObjectGDIP.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    graphicsObjectGDIP.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                }


                graphicsObject.Clear(Color.Gray);

                if (_bgimage != null)
                {
                    bgon = false;
                    graphicsObject.DrawImage(_bgimage, 0, 0, this.Width, this.Height);

                    if (hudon == false)
                    {
                        return;
                    }
                }
                else
                {
                    bgon = true;
                }

                graphicsObject.TranslateTransform(this.Width / 2, this.Height / 2);

                graphicsObject.RotateTransform(-roll);

                int fontsize = this.Height / 30; // = 10
                int fontoffset = fontsize - 10;

                float every5deg = -this.Height / 60;

                float pitchoffset = -pitch * every5deg;

                int halfwidth = this.Width / 2;
                int halfheight = this.Height / 2;

                SolidBrush whiteBrush = new SolidBrush(whitePen.Color);

                Pen blackPen = new Pen(Color.Black, 2);
                Pen greenPen = new Pen(Color.Green, 2);
                Pen redPen = new Pen(Color.Red, 2);

                // draw sky
                if (bgon == true)
                {
                    RectangleF bg = new RectangleF(-halfwidth * 2, -halfheight * 2, this.Width * 2, halfheight * 2 + pitchoffset);

                    if (bg.Height != 0)
                    {
                        LinearGradientBrush linearBrush = new LinearGradientBrush(bg, Color.Blue,
                            Color.LightBlue, LinearGradientMode.Vertical);

                        graphicsObject.FillRectangle(linearBrush, bg);
                    }
                    // draw ground

                    bg = new RectangleF(-halfwidth * 2, pitchoffset, this.Width * 2, halfheight * 2 - pitchoffset);

                    if (bg.Height != 0)
                    {
                        LinearGradientBrush linearBrush = new LinearGradientBrush(bg, Color.FromArgb(0x9b, 0xb8, 0x24),
                            Color.FromArgb(0x41, 0x4f, 0x07), LinearGradientMode.Vertical);

                        graphicsObject.FillRectangle(linearBrush, bg);
                    }

                    //draw centerline
                    graphicsObject.DrawLine(whitePen, -halfwidth * 2, pitchoffset + 0, halfwidth * 2, pitchoffset + 0);
                }

                graphicsObject.ResetTransform();

                graphicsObject.SetClip(new Rectangle(0, this.Height / 14, this.Width, this.Height - this.Height / 14));

                graphicsObject.TranslateTransform(this.Width / 2, this.Height / 2);
                graphicsObject.RotateTransform(-roll);

                //draw pitch           

                int lengthshort = this.Width / 12;
                int lengthlong = this.Width / 8;

                for (int a = -90; a <= 90; a += 5)
                {
                    // limit to 40 degrees
                    if (a >= pitch - 34 && a <= pitch + 25)
                    {
                        if (a % 10 == 0)
                        {
                            if (a == 0)
                            {
                                graphicsObject.DrawLine(greenPen, this.Width / 2 - lengthlong - halfwidth, pitchoffset + a * every5deg, this.Width / 2 + lengthlong - halfwidth, pitchoffset + a * every5deg);
                            }
                            else
                            {
                                graphicsObject.DrawLine(whitePen, this.Width / 2 - lengthlong - halfwidth, pitchoffset + a * every5deg, this.Width / 2 + lengthlong - halfwidth, pitchoffset + a * every5deg);
                            }
                            drawstring(graphicsObject, a.ToString(), font, fontsize + 2, whiteBrush, this.Width / 2 - lengthlong - 30 - halfwidth - (int)(fontoffset * 1.7), pitchoffset + a * every5deg - 8 - fontoffset);
                        }
                        else
                        {
                            graphicsObject.DrawLine(whitePen, this.Width / 2 - lengthshort - halfwidth, pitchoffset + a * every5deg, this.Width / 2 + lengthshort - halfwidth, pitchoffset + a * every5deg);
                            //drawstring(e,a.ToString(), new Font("Arial", 10), whiteBrush, this.Width / 2 - lengthshort - 20 - halfwidth, this.Height / 2 + pitchoffset + a * every5deg - 8);
                        }
                    }
                }

                graphicsObject.ResetTransform();

                // draw roll ind needle

                graphicsObject.TranslateTransform(this.Width / 2, this.Height / 2 + this.Height / 14);

                graphicsObject.RotateTransform(-roll);

                Point[] pointlist = new Point[3];

                lengthlong = this.Height / 66;

                int extra = this.Height / 15 * 7;

                pointlist[0] = new Point(0, -lengthlong * 2 - extra);
                pointlist[1] = new Point(-lengthlong, -lengthlong - extra);
                pointlist[2] = new Point(lengthlong, -lengthlong - extra);

                if (Math.Abs(roll) > 45)
                {
                    redPen.Width = 10;
                }

                graphicsObject.DrawPolygon(redPen, pointlist);

                redPen.Width = 2;

                for (int a = -45; a <= 45; a += 15)
                {
                    graphicsObject.ResetTransform();
                    graphicsObject.TranslateTransform(this.Width / 2, this.Height / 2 + this.Height / 14);
                    graphicsObject.RotateTransform(a);
                    drawstring(graphicsObject, Math.Abs(a).ToString("##"), font, fontsize, whiteBrush, 0 - 6 - fontoffset, -lengthlong * 2 - extra);
                    graphicsObject.DrawLine(whitePen, 0, -halfheight, 0, -halfheight - 10);
                }

                graphicsObject.ResetTransform();

                //draw centre / current att

                Rectangle centercircle = new Rectangle(halfwidth - 10, halfheight - 10, 20, 20);

                graphicsObject.DrawEllipse(redPen, centercircle);
                graphicsObject.DrawLine(redPen, centercircle.Left - 10, halfheight, centercircle.Left, halfheight);
                graphicsObject.DrawLine(redPen, centercircle.Right, halfheight, centercircle.Right + 10, halfheight);
                graphicsObject.DrawLine(redPen, centercircle.Left + centercircle.Width / 2, centercircle.Top, centercircle.Left + centercircle.Width / 2, centercircle.Top - 10);

                // draw roll ind

                Rectangle arcrect = new Rectangle(this.Width / 2 - this.Height / 2, this.Height / 14, this.Height, this.Height);

                graphicsObject.DrawArc(whitePen, arcrect, 180 + 45, 90);

                //draw heading ind

                graphicsObject.ResetClip();

                Rectangle headbg = new Rectangle(0, 0, this.Width - 0, this.Height / 14);

                graphicsObject.DrawRectangle(blackPen, headbg);

                SolidBrush solidBrush = new SolidBrush(Color.FromArgb(0x55, 0xff, 0xff, 0xff));

                graphicsObject.FillRectangle(solidBrush, headbg);

                // center
                graphicsObject.DrawLine(redPen, headbg.Width / 2, headbg.Bottom, headbg.Width / 2, headbg.Top);

                //bottom line
                graphicsObject.DrawLine(whitePen, headbg.Left + 5, headbg.Bottom - 5, headbg.Width - 5, headbg.Bottom - 5);

                float space = (headbg.Width - 10) / 60.0f;
                int start = (int)Math.Round((heading - 30),1);

                // draw for outside the 60 deg
                if (targetheading < start)
                {
                    greenPen.Width = 6;
                    graphicsObject.DrawLine(greenPen, headbg.Left + 5 + space * 0, headbg.Bottom, headbg.Left + 5 + space * (0), headbg.Top);
                }
                if (targetheading > heading + 30)
                {
                    greenPen.Width = 6;
                    graphicsObject.DrawLine(greenPen, headbg.Left + 5 + space * 60, headbg.Bottom, headbg.Left + 5 + space * (60), headbg.Top);
                }

                for (int a = start; a <= heading + 30; a += 1)
                {
                    // target heading
                    if (((int)(a + 360) % 360) == (int)targetheading)
                    {
                        greenPen.Width = 6;
                        graphicsObject.DrawLine(greenPen, headbg.Left + 5 + space * (a - start), headbg.Bottom, headbg.Left + 5 + space * (a - start), headbg.Top);
                    }

                    if (((int)(a + 360) % 360) == (int)groundcourse)
                    {
                        blackPen.Width = 6;
                        graphicsObject.DrawLine(blackPen, headbg.Left + 5 + space * (a - start), headbg.Bottom, headbg.Left + 5 + space * (a - start), headbg.Top);
                        blackPen.Width = 2;
                    }

                    if ((int)a % 5 == 0)
                    {
                        //Console.WriteLine(a + " " + Math.Round(a, 1, MidpointRounding.AwayFromZero));
                        //Console.WriteLine(space +" " + a +" "+ (headbg.Left + 5 + space * (a - start)));
                        graphicsObject.DrawLine(whitePen, headbg.Left + 5 + space * (a - start), headbg.Bottom - 5, headbg.Left + 5 + space * (a - start), headbg.Bottom - 10);
                        int disp = (int)a;
                        if (disp < 0)
                            disp += 360;
                        disp = disp % 360;
                        if (disp == 0)
                        {
                            drawstring(graphicsObject, "N".PadLeft(2), font, fontsize + 4, whiteBrush, headbg.Left - 5 + space * (a - start) - fontoffset, headbg.Bottom - 24 - (int)(fontoffset * 1.7));
                        }
                        else if (disp == 90)
                        {
                            drawstring(graphicsObject, "E".PadLeft(2), font, fontsize + 4, whiteBrush, headbg.Left - 5 + space * (a - start) - fontoffset, headbg.Bottom - 24 - (int)(fontoffset * 1.7));
                        }
                        else if (disp == 180)
                        {
                            drawstring(graphicsObject, "S".PadLeft(2), font, fontsize + 4, whiteBrush, headbg.Left - 5 + space * (a - start) - fontoffset, headbg.Bottom - 24 - (int)(fontoffset * 1.7));
                        }
                        else if (disp == 270)
                        {
                            drawstring(graphicsObject, "W".PadLeft(2), font, fontsize + 4, whiteBrush, headbg.Left - 5 + space * (a - start) - fontoffset, headbg.Bottom - 24 - (int)(fontoffset * 1.7));
                        }
                        else
                        {
                            drawstring(graphicsObject, (disp % 360).ToString().PadLeft(3), font, fontsize, whiteBrush, headbg.Left - 5 + space * (a - start) - fontoffset, headbg.Bottom - 24 - (int)(fontoffset * 1.7));
                        }
                    }
                }

                //                Console.WriteLine("HUD 0 " + (DateTime.Now - starttime).TotalMilliseconds + " " + DateTime.Now.Millisecond);

                // xtrack error
                // center

                float xtspace = this.Width / 10.0f / 3.0f;
                int pad = 10;

                float myxtrack_error = xtrack_error;

                myxtrack_error = Math.Min(myxtrack_error, 40);
                myxtrack_error = Math.Max(myxtrack_error, -40);

                //  xtrack - distance scale - space
                float loc = myxtrack_error / 20.0f * xtspace;

                // current xtrack
                if (Math.Abs(myxtrack_error) == 40)
                {
                    greenPen.Color = Color.FromArgb(128, greenPen.Color);
                }

                graphicsObject.DrawLine(greenPen, this.Width / 10 + loc, headbg.Bottom + 5, this.Width / 10 + loc, headbg.Bottom + this.Height / 10);

                greenPen.Color = Color.FromArgb(255, greenPen.Color);

                graphicsObject.DrawLine(whitePen, this.Width / 10, headbg.Bottom + 5, this.Width / 10, headbg.Bottom + this.Height / 10);

                graphicsObject.DrawLine(whitePen, this.Width / 10 - xtspace, headbg.Bottom + 5 + pad, this.Width / 10 - xtspace, headbg.Bottom + this.Height / 10 - pad);

                graphicsObject.DrawLine(whitePen, this.Width / 10 - xtspace * 2, headbg.Bottom + 5 + pad, this.Width / 10 - xtspace * 2, headbg.Bottom + this.Height / 10 - pad);

                graphicsObject.DrawLine(whitePen, this.Width / 10 + xtspace, headbg.Bottom + 5 + pad, this.Width / 10 + xtspace, headbg.Bottom + this.Height / 10 - pad);

                graphicsObject.DrawLine(whitePen, this.Width / 10 + xtspace * 2, headbg.Bottom + 5 + pad, this.Width / 10 + xtspace * 2, headbg.Bottom + this.Height / 10 - pad);

                // rate of turn

                whitePen.Width = 4;
                graphicsObject.DrawLine(whitePen, this.Width / 10 - xtspace * 2 - xtspace / 2, headbg.Bottom + this.Height / 10 + 10, this.Width / 10 - xtspace * 2 - xtspace / 2 + xtspace, headbg.Bottom + this.Height / 10 + 10);

                graphicsObject.DrawLine(whitePen, this.Width / 10 - xtspace * 0 - xtspace / 2, headbg.Bottom + this.Height / 10 + 10, this.Width / 10 - xtspace * 0 - xtspace / 2 + xtspace, headbg.Bottom + this.Height / 10 + 10);

                graphicsObject.DrawLine(whitePen, this.Width / 10 + xtspace * 2 - xtspace / 2, headbg.Bottom + this.Height / 10 + 10, this.Width / 10 + xtspace * 2 - xtspace / 2 + xtspace, headbg.Bottom + this.Height / 10 + 10);

                float myturnrate = turnrate;
                float trwidth = (this.Width / 10 + xtspace * 2 - xtspace / 2) - (this.Width / 10 - xtspace * 2 - xtspace / 2);

                float range = 12;

                myturnrate = Math.Min(myturnrate, range / 2);
                myturnrate = Math.Max(myturnrate, (range / 2) * -1.0f);

                loc = myturnrate / range * trwidth;

                greenPen.Width = 4;

                if (Math.Abs(myturnrate) == (range / 2))
                {
                    greenPen.Color = Color.FromArgb(128, greenPen.Color);
                }

                graphicsObject.DrawLine(greenPen, this.Width / 10 + loc - xtspace / 2, headbg.Bottom + this.Height / 10 + 10 + 3, this.Width / 10 + loc + xtspace / 2, headbg.Bottom + this.Height / 10 + 10 + 3);
                graphicsObject.DrawLine(greenPen, this.Width / 10 + loc, headbg.Bottom + this.Height / 10 + 10 + 3, this.Width / 10 + loc, headbg.Bottom + this.Height / 10 + 10 + 10);

                greenPen.Color = Color.FromArgb(255, greenPen.Color);

                whitePen.Width = 2;



                // left scroller

                Rectangle scrollbg = new Rectangle(0, halfheight - halfheight / 2, this.Width / 10, this.Height / 2);

                graphicsObject.DrawRectangle(whitePen, scrollbg);

                graphicsObject.FillRectangle(solidBrush, scrollbg);

                Point[] arrow = new Point[5];

                arrow[0] = new Point(0, -10);
                arrow[1] = new Point(scrollbg.Width - 10, -10);
                arrow[2] = new Point(scrollbg.Width - 5, 0);
                arrow[3] = new Point(scrollbg.Width - 10, 10);
                arrow[4] = new Point(0, 10);

                graphicsObject.TranslateTransform(0, this.Height / 2);

                int viewrange = 26;

                float speed = airspeed;
                if (speed == 0)
                    speed = groundspeed;

                space = (scrollbg.Height) / (float)viewrange;
                start = ((int)speed - viewrange / 2);

                if (start > targetspeed)
                {
                    greenPen.Color = Color.FromArgb(128, greenPen.Color);
                    greenPen.Width = 6;
                    graphicsObject.DrawLine(greenPen, scrollbg.Left, scrollbg.Top, scrollbg.Left + scrollbg.Width, scrollbg.Top);
                    greenPen.Color = Color.FromArgb(255, greenPen.Color);
                }
                if ((speed + viewrange / 2) < targetspeed)
                {
                    greenPen.Color = Color.FromArgb(128, greenPen.Color);
                    greenPen.Width = 6;
                    graphicsObject.DrawLine(greenPen, scrollbg.Left, scrollbg.Top - space * viewrange, scrollbg.Left + scrollbg.Width, scrollbg.Top - space * viewrange);
                    greenPen.Color = Color.FromArgb(255, greenPen.Color);
                }

                for (int a = (int)start; a <= (speed + viewrange / 2); a += 1)
                {
                    if (a == (int)targetspeed && targetspeed != 0)
                    {
                        greenPen.Width = 6;
                        graphicsObject.DrawLine(greenPen, scrollbg.Left, scrollbg.Top - space * (a - start), scrollbg.Left + scrollbg.Width, scrollbg.Top - space * (a - start));
                    }
                    if (a % 5 == 0)
                    {
                        //Console.WriteLine(a + " " + scrollbg.Right + " " + (scrollbg.Top - space * (a - start)) + " " + (scrollbg.Right - 20) + " " + (scrollbg.Top - space * (a - start)));
                        graphicsObject.DrawLine(whitePen, scrollbg.Right, scrollbg.Top - space * (a - start), scrollbg.Right - 10, scrollbg.Top - space * (a - start));
                        drawstring(graphicsObject, a.ToString().PadLeft(5), font, fontsize, whiteBrush, scrollbg.Right - 50 - 4 * fontoffset, scrollbg.Top - space * (a - start) - 6 - fontoffset);
                    }
                }

                graphicsObject.DrawPolygon(blackPen, arrow);
                graphicsObject.FillPolygon(Brushes.Black, arrow);
                drawstring(graphicsObject, ((int)speed).ToString("0"), font, 10, Brushes.AliceBlue, 0, -9);

                graphicsObject.ResetTransform();

                // extra text data

                drawstring(graphicsObject, "AS " + airspeed.ToString("0.0"), font, fontsize, whiteBrush, 1, scrollbg.Bottom + 5);
                drawstring(graphicsObject, "GS " + groundspeed.ToString("0.0"), font, fontsize, whiteBrush, 1, scrollbg.Bottom + fontsize + 2 + 10);

                //drawstring(e,, new Font("Arial", fontsize + 2), whiteBrush, 1, scrollbg.Bottom + fontsize + 2 + 10);

                // right scroller

                scrollbg = new Rectangle(this.Width - this.Width / 10, halfheight - halfheight / 2, this.Width / 10, this.Height / 2);

                graphicsObject.DrawRectangle(whitePen, scrollbg);

                graphicsObject.FillRectangle(solidBrush, scrollbg);

                arrow = new Point[5];

                arrow[0] = new Point(0, -10);
                arrow[1] = new Point(scrollbg.Width - 10, -10);
                arrow[2] = new Point(scrollbg.Width - 5, 0);
                arrow[3] = new Point(scrollbg.Width - 10, 10);
                arrow[4] = new Point(0, 10);



                graphicsObject.TranslateTransform(0, this.Height / 2);




                viewrange = 26;

                space = (scrollbg.Height) / (float)viewrange;
                start = ((int)alt - viewrange / 2);

                if (start > targetalt)
                {
                    greenPen.Color = Color.FromArgb(128, greenPen.Color);
                    greenPen.Width = 6;
                    graphicsObject.DrawLine(greenPen, scrollbg.Left, scrollbg.Top, scrollbg.Left + scrollbg.Width, scrollbg.Top);
                    greenPen.Color = Color.FromArgb(255, greenPen.Color);
                }
                if ((alt + viewrange / 2) < targetalt)
                {
                    greenPen.Color = Color.FromArgb(128, greenPen.Color);
                    greenPen.Width = 6;
                    graphicsObject.DrawLine(greenPen, scrollbg.Left, scrollbg.Top - space * viewrange, scrollbg.Left + scrollbg.Width, scrollbg.Top - space * viewrange);
                    greenPen.Color = Color.FromArgb(255, greenPen.Color);
                }

                for (int a = (int)start; a <= (alt + viewrange / 2); a += 1)
                {
                    if (a == Math.Round(targetalt) && targetalt != 0)
                    {
                        greenPen.Width = 6;
                        graphicsObject.DrawLine(greenPen, scrollbg.Left, scrollbg.Top - space * (a - start), scrollbg.Left + scrollbg.Width, scrollbg.Top - space * (a - start));
                    }
                    if (a % 5 == 0)
                    {
                        //Console.WriteLine(a + " " + scrollbg.Left + " " + (scrollbg.Top - space * (a - start)) + " " + (scrollbg.Left + 20) + " " + (scrollbg.Top - space * (a - start)));
                        graphicsObject.DrawLine(whitePen, scrollbg.Left, scrollbg.Top - space * (a - start), scrollbg.Left + 10, scrollbg.Top - space * (a - start));
                        drawstring(graphicsObject, a.ToString().PadLeft(5), font, fontsize, whiteBrush, scrollbg.Left + 7 + (int)(0 * fontoffset), scrollbg.Top - space * (a - start) - 6 - fontoffset);
                    }
                }

                greenPen.Width = 4;

                // vsi

                graphicsObject.ResetTransform();

                PointF[] poly = new PointF[4];

                poly[0] = new PointF(scrollbg.Left, scrollbg.Top);
                poly[1] = new PointF(scrollbg.Left - scrollbg.Width / 4, scrollbg.Top + scrollbg.Width / 4);
                poly[2] = new PointF(scrollbg.Left - scrollbg.Width / 4, scrollbg.Bottom - scrollbg.Width / 4);
                poly[3] = new PointF(scrollbg.Left, scrollbg.Bottom);

                //verticalspeed

                viewrange = 12;

                verticalspeed = Math.Min(viewrange / 2, verticalspeed);
                verticalspeed = Math.Max(viewrange / -2, verticalspeed);

                float scaledvalue = verticalspeed / -viewrange * (scrollbg.Bottom - scrollbg.Top);

                float linespace = (float)1 / -viewrange * (scrollbg.Bottom - scrollbg.Top);

                PointF[] polyn = new PointF[4];

                polyn[0] = new PointF(scrollbg.Left, scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2);
                polyn[1] = new PointF(scrollbg.Left - scrollbg.Width / 4, scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2);
                polyn[2] = polyn[1];
                float peak = 0;
                if (scaledvalue > 0)
                {
                    peak = -scrollbg.Width / 4;
                    if (scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2 + scaledvalue + peak < scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2)
                        peak = -scaledvalue;
                }
                else if (scaledvalue < 0)
                {
                    peak = +scrollbg.Width / 4;
                    if (scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2 + scaledvalue + peak > scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2)
                        peak = -scaledvalue;
                }
                
                polyn[2] = new PointF(scrollbg.Left - scrollbg.Width / 4, scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2 + scaledvalue + peak);
                polyn[3] = new PointF(scrollbg.Left, scrollbg.Top + (scrollbg.Bottom - scrollbg.Top) / 2 + scaledvalue);

                //graphicsObject.DrawPolygon(redPen, poly);
                graphicsObject.FillPolygon(Brushes.Blue, polyn);

                // draw outsidebox
                graphicsObject.DrawPolygon(whitePen, poly);

                for (int a = 1; a < viewrange; a++)
                {
                    graphicsObject.DrawLine(whitePen, scrollbg.Left - scrollbg.Width / 4, scrollbg.Top - linespace * a, scrollbg.Left - scrollbg.Width / 8, scrollbg.Top - linespace * a);
                }

                // draw arrow and text
                
                graphicsObject.ResetTransform();
                graphicsObject.TranslateTransform(this.Width, this.Height / 2);
                graphicsObject.RotateTransform(180);

                graphicsObject.DrawPolygon(blackPen, arrow);
                graphicsObject.FillPolygon(Brushes.Black, arrow);
                graphicsObject.ResetTransform();
                graphicsObject.TranslateTransform(0, this.Height / 2);

                drawstring(graphicsObject, ((int)alt).ToString("0"), font, 10, Brushes.AliceBlue, scrollbg.Left + 10, -9);
                graphicsObject.ResetTransform();

                // mode and wp dist and wp

                drawstring(graphicsObject, mode, font, fontsize, whiteBrush, scrollbg.Left - 30, scrollbg.Bottom + 5);
                drawstring(graphicsObject, (int)disttowp + ">" + wpno, font, fontsize, whiteBrush, scrollbg.Left - 30, scrollbg.Bottom + fontsize + 2 + 10);

                graphicsObject.DrawLine(greenPen, scrollbg.Left - 5, scrollbg.Top - (int)(fontsize * 2.2) - 2 - 20, scrollbg.Left - 5, scrollbg.Top - (int)(fontsize) - 2 - 20);
                graphicsObject.DrawLine(greenPen, scrollbg.Left - 10, scrollbg.Top - (int)(fontsize * 2.2) - 2 - 15, scrollbg.Left - 10, scrollbg.Top - (int)(fontsize) - 2 - 20);
                graphicsObject.DrawLine(greenPen, scrollbg.Left - 15, scrollbg.Top - (int)(fontsize * 2.2) - 2 - 10, scrollbg.Left - 15, scrollbg.Top - (int)(fontsize ) - 2 - 20);

                drawstring(graphicsObject, ArdupilotMega.MainV2.cs.linkqualitygcs.ToString("0") + "%", font, fontsize, whiteBrush, scrollbg.Left, scrollbg.Top - (int)(fontsize * 2.2) - 2 - 20);
                if (ArdupilotMega.MainV2.cs.linkqualitygcs == 0)
                {
                    graphicsObject.DrawLine(redPen, scrollbg.Left, scrollbg.Top - (int)(fontsize * 2.2) - 2 - 20, scrollbg.Left + 50, scrollbg.Top - (int)(fontsize * 2.2) - 2);

                    graphicsObject.DrawLine(redPen, scrollbg.Left, scrollbg.Top - (int)(fontsize * 2.2) - 2, scrollbg.Left + 50, scrollbg.Top - (int)(fontsize * 2.2) - 2 - 20);
                }
                drawstring(graphicsObject, ArdupilotMega.MainV2.cs.datetime.ToString("HH:mm:ss"), font, fontsize, whiteBrush, scrollbg.Left - 20, scrollbg.Top - fontsize - 2 - 20);


                // battery

                graphicsObject.ResetTransform();

                drawstring(graphicsObject, resources.GetString("Bat"), font, fontsize + 2, whiteBrush, fontsize, this.Height - 30 - fontoffset);
                drawstring(graphicsObject, batterylevel.ToString("0.00v"), font, fontsize + 2, whiteBrush, fontsize * 4, this.Height - 30 - fontoffset);
                drawstring(graphicsObject, batteryremaining.ToString("0%"), font, fontsize + 2, whiteBrush, fontsize * 9, this.Height - 30 - fontoffset);

                // gps

                string gps = "";

                if (gpsfix == 0)
                {
                    gps = resources.GetString("GPS: No GPS.Text");
                }
                else if (gpsfix == 1)
                {
                    gps = resources.GetString("GPS: No Fix.Text");
                }
                else if (gpsfix == 2)
                {
                    gps = resources.GetString("GPS: 2D Fix.Text");
                }
                else if (gpsfix == 3)
                {
                    gps = resources.GetString("GPS: 3D Fix.Text");
                }

                drawstring(graphicsObject, gps, font, fontsize + 2, whiteBrush, this.Width - 10 * fontsize, this.Height - 30 - fontoffset);

                if (!opengl)
                {
                    e.Graphics.DrawImageUnscaled(objBitmap, 0, 0);
                }

                if (DesignMode)
                {
                    return;
                }

                //                Console.WriteLine("HUD 1 " + (DateTime.Now - starttime).TotalMilliseconds + " " + DateTime.Now.Millisecond);

                ImageCodecInfo ici = GetImageCodec("image/jpeg");
                EncoderParameters eps = new EncoderParameters(1);
                eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L); // or whatever other quality value you want

                lock (streamlock)
                {
                    if (streamjpgenable || streamjpg == null) // init image and only update when needed
                    {
                        if (opengl)
                        {
                            objBitmap = GrabScreenshot();
                        }
                        streamjpg = new MemoryStream();
                        objBitmap.Save(streamjpg, ici, eps);
                        //objBitmap.Save(streamjpg,ImageFormat.Bmp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("hud error "+ex.ToString());
                //MessageBox.Show(ex.ToString());            
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //base.OnPaintBackground(e);
        }

        ImageCodecInfo GetImageCodec(string mimetype)
        {
            foreach (ImageCodecInfo ici in ImageCodecInfo.GetImageEncoders())
            {
                if (ici.MimeType == mimetype) return ici;
            }
            return null;
        }

        // Returns a System.Drawing.Bitmap with the contents of the current framebuffer
        public new Bitmap GrabScreenshot()
        {
            if (GraphicsContext.CurrentContext == null)
                throw new GraphicsContextMissingException();

            Bitmap bmp = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(this.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, this.ClientSize.Width, this.ClientSize.Height,OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bmp;
        }


        float wrap360(float noin)
        {
            if (noin < 0)
                return noin + 360;
            return noin;
        }

        /// <summary>
        /// pen for drawstring
        /// </summary>
        Pen P = new Pen(Color.FromArgb(0x26, 0x27, 0x28), 2f);
        /// <summary>
        /// pth for drawstring
        /// </summary>
        GraphicsPath pth = new GraphicsPath();

        void drawstring(HUD e, string text, Font font, float fontsize, Brush brush, float x, float y)
        {
            if (!opengl)
            {
                drawstring(graphicsObjectGDIP, text, font, fontsize, brush, x, y);
                return;
            }

            if (text == null || text == "")
                return;
            /*
            OpenTK.Graphics.Begin(); 
            GL.PushMatrix(); 
            GL.Translate(x, y, 0);
            printer.Print(text, font, c); 
            GL.PopMatrix(); printer.End();
            */

            char[] chars = text.ToCharArray();

            float maxy = 1;

            foreach (char cha in chars)
            {
                int charno = (int)cha;

                int charid = charno  + (128 * (int)fontsize); // 128 * 40 * 5;128

                if (charbitmaps[charid] == null)
                {
                    charbitmaps[charid] = new Bitmap(128, 128, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    charbitmaps[charid].MakeTransparent(Color.Transparent);

                    //charbitmaptexid

                    float maxx = this.Width / 150; // for space


                    // create bitmap
                    using (Graphics gfx = Graphics.FromImage(charbitmaps[charid]))
                    {
                        pth.Reset();

                        if (text != null)
                            pth.AddString(cha + "", font.FontFamily, 0, fontsize + 5, new Point((int)0, (int)0), StringFormat.GenericTypographic);

                        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                        gfx.DrawPath(P, pth);

                        //Draw the face

                        gfx.FillPath(brush, pth);


                        if (pth.PointCount > 0)
                        {
                            foreach (PointF pnt in pth.PathPoints)
                            {
                                if (pnt.X > maxx)
                                    maxx = pnt.X;

                                if (pnt.Y > maxy)
                                    maxy = pnt.Y;
                            }
                        }
                    }

                    charwidth[charid] = (int)(maxx + 2);

                    //charbitmaps[charid] = charbitmaps[charid].Clone(new RectangleF(0, 0, maxx + 2, maxy + 2), charbitmaps[charid].PixelFormat);

                    //charbitmaps[charno * (int)fontsize].Save(charno + " " + (int)fontsize + ".png");

                    // create texture
                    int textureId;
                    GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvModeCombine.Replace);//Important, or wrong color on some computers

                    Bitmap bitmap = charbitmaps[charid];
                    GL.GenTextures(1, out textureId);
                    GL.BindTexture(TextureTarget.Texture2D, textureId);

                    BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                    //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
                    //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                    GL.Finish();
                    bitmap.UnlockBits(data);

                    charbitmaptexid[charid] = textureId;
                }

                //GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, charbitmaptexid[charid]);

                float scale = 1.0f;

                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(0, 0); GL.Vertex2(x, y);
                GL.TexCoord2(1, 0); GL.Vertex2(x + charbitmaps[charid].Width * scale, y);
                GL.TexCoord2(1, 1); GL.Vertex2(x + charbitmaps[charid].Width * scale, y + charbitmaps[charid].Height * scale);
                GL.TexCoord2(0, 1); GL.Vertex2(x + 0, y + charbitmaps[charid].Height * scale);
                GL.End();

                //GL.Disable(EnableCap.Blend);
                GL.Disable(EnableCap.Texture2D);

                x += charwidth[charid] * scale;
            }
        }
		
		void drawstring(Graphics e, string text, Font font, float fontsize, Brush brush, float x, float y)
        {
            if (text == null || text == "")
                return;

            pth.Reset();


            if (text != null)
                pth.AddString(text, font.FontFamily, 0, fontsize + 5, new Point((int)x, (int)y), StringFormat.GenericTypographic);

            //Draw the edge
            // this uses lots of cpu time

            //e.SmoothingMode = SmoothingMode.HighSpeed;
            
            e.DrawPath(P, pth);

            //Draw the face

            e.FillPath(brush, pth);

            //pth.Dispose();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            try
            {
                if (opengl)
                {
                    base.OnHandleCreated(e);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); opengl = false; } // macs fail here
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                if (opengl)
                {
                    base.OnHandleDestroyed(e);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); opengl = false; }
        }

        protected override void OnResize(EventArgs e)
        {
            if (DesignMode)
                return;
            this.Height = (int)(this.Width / 1.333f);
            base.OnResize(e);

            graphicsObjectGDIP = Graphics.FromImage(objBitmap);

            charbitmaps = new Bitmap[charbitmaps.Length];

            try
            {
                if (opengl)
                {
                    foreach (int texid in charbitmaptexid)
                    {
                        if (texid != 0)
                            GL.DeleteTexture(texid);
                    }
                }
            }
            catch { }

            GC.Collect();
            
            try
            {
                if (opengl)
                {
                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    GL.Ortho(0, Width, Height, 0, -1, 1);
                    GL.MatrixMode(MatrixMode.Modelview);
                    GL.LoadIdentity();

                    GL.Viewport(0, 0, Width, Height);
                }
            }
            catch { }
             
        }
    }
}