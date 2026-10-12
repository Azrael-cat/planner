using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace DeskTasks  {
    public static class Theme  {
        public static Color Ink=Color.FromArgb(40,43,64), Muted=Color.FromArgb(127,132,151), Purple=Color.FromArgb(112,93,223), Canvas=Color.FromArgb(247,247,252), Line=Color.FromArgb(232,233,241), Mint=Color.FromArgb(43,157,127);
        public static Font Font(float size, bool bold=false) {
            return new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular);
        }
        public static GraphicsPath Round(RectangleF r,float radius) {
            var p=new GraphicsPath();
            float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
            p.AddArc(r.X,r.Y,d,d,180,90);
            p.AddArc(r.Right-d,r.Y,d,d,270,90);
            p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);
            p.AddArc(r.X,r.Bottom-d,d,d,90,90);
            p.CloseFigure();
            return p;
        }
        public static void Fill(Graphics g,RectangleF r,Color color,int radius) {
            using(var path=Round(r,radius))using(var b=new SolidBrush(color))g.FillPath(b,path);
        }
        public static void Text(Graphics g,string text,Font font,Color color,Rectangle rect,TextFormatFlags flags=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis) {
            TextRenderer.DrawText(g,text,font,rect,color,flags|TextFormatFlags.NoPrefix);
        }
        public static void RoundWindow(Form form,int radius) {
            Action apply=()=> {
                if(form.Width<=0||form.Height<=0)return;
                using(var path=Round(new Rectangle(0,0,form.Width,form.Height),radius)) {
                    Region old=form.Region;
                    form.Region=new Region(path);
                    if(old!=null)old.Dispose();
                }
            }
            ;
            apply();
            form.SizeChanged+=(o,e)=>apply();
        }
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        public static Icon MakeIcon() {
            using(var bmp=new Bitmap(32,32)) {
                using(var g=Graphics.FromImage(bmp)) {
                    g.SmoothingMode=SmoothingMode.AntiAlias;
                    Fill(g,new Rectangle(1,1,30,30),Purple,9);
                    using(var pen=new Pen(Color.White,3)) {
                        pen.StartCap=pen.EndCap=LineCap.Round;
                        g.DrawLines(pen,new Point[] {
                            new Point(9,16),new Point(14,21),new Point(24,11)
                        }
                        );
                    }
                }
                IntPtr h=bmp.GetHicon();
                try {
                    return (Icon)Icon.FromHandle(h).Clone();
                }
                finally {
                    DestroyIcon(h);
                }
            }
        }
        public static void Apply(Control parent) {
            parent.ForeColor=Ink;
            if(parent is Form) {
                parent.BackColor=Canvas;
                ((Form)parent).Icon=MakeIcon();
            }
            foreach(Control c in parent.Controls) {
                if(c is ModernButton)continue;
                if(c is TextBox) {
                    ((TextBox)c).BorderStyle=BorderStyle.FixedSingle;
                    c.BackColor=Color.White;
                }
                if(c is Label)c.ForeColor=Muted;
                if(c is ListView)StyleList((ListView)c);
                if(c is TabControl)StyleTabs((TabControl)c);
                Apply(c);
            }
        }
        static void StyleTabs(TabControl tabs) {
            tabs.DrawMode=TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize=new Size(145,44);
            tabs.SizeMode=TabSizeMode.Fixed;
            tabs.Padding=new Point(18,10);
            tabs.DrawItem+=(o,e)=> {
                Rectangle r=tabs.GetTabRect(e.Index);
                bool active=e.Index==tabs.SelectedIndex;
                using(var b=new SolidBrush(Canvas))e.Graphics.FillRectangle(b,r);
                if(active)Fill(e.Graphics,new Rectangle(r.X+5,r.Y+5,r.Width-10,r.Height-9),Color.FromArgb(235,231,253),9);
                using(var f=Font(10,active))Text(e.Graphics,tabs.TabPages[e.Index].Text,f,active?Purple:Muted,r,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
            ;
        }
        static void StyleList(ListView list) {
            list.BorderStyle=BorderStyle.None;
            list.HideSelection=false;
            list.BackColor=Color.White;
            list.OwnerDraw=true;
            var images=new ImageList {
                ImageSize=new Size(1,42)
            }
            ;
            images.Images.Add(new Bitmap(1,42));
            list.SmallImageList=images;
            list.DrawColumnHeader+=(o,e)=> {
                using(var b=new SolidBrush(Canvas))e.Graphics.FillRectangle(b,e.Bounds);
                using(var f=Font(9))Text(e.Graphics,e.Header.Text,f,Muted,new Rectangle(e.Bounds.X+12,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height));
            }
            ;
            list.DrawItem+=(o,e)=> {
            }
            ;
            list.DrawSubItem+=(o,e)=> {
                bool selected=e.Item.Selected;
                Color bg=selected?Color.FromArgb(240,237,254):(e.ItemIndex%2==0?Color.White:Color.FromArgb(251,251,254));
                using(var b=new SolidBrush(bg))e.Graphics.FillRectangle(b,e.Bounds);
                using(var f=Font(9))Text(e.Graphics,e.SubItem.Text,f,e.ColumnIndex==0?Ink:Muted,new Rectangle(e.Bounds.X+12,e.Bounds.Y,e.Bounds.Width-18,e.Bounds.Height));
                if((e.Item.Tag is Project||e.Item.Tag is ScheduleGroup)&&e.ColumnIndex==1) {
                    string[] parts=e.SubItem.Text.Split('/');
                    int a,b;
                    if(parts.Length==2&&Int32.TryParse(parts[0].Trim(),out a)&&Int32.TryParse(parts[1].Trim(),out b)&&b>0) {
                        Rectangle bar=new Rectangle(e.Bounds.X+12,e.Bounds.Bottom-7,e.Bounds.Width-28,3);
                        Fill(e.Graphics,bar,Line,1);
                        if(a>0)Fill(e.Graphics,new Rectangle(bar.X,bar.Y,Math.Max(2,bar.Width*a/b),3),Purple,1);
                    }
                }
            }
            ;
        }
    }
    public class StatsMetrics:Control  {
        public string Progress="0 / 0", Today="0%", Speed="0";
        public StatsMetrics() {
            DoubleBuffered=true;
            Height=118;
        }
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            string[] names= {
                "累计完成 / 总份数","今日完成率","每计划小时完成"
            }
            ;
            string[] values= {
                Progress,Today,Speed
            }
            ;
            int w=Width/3;
            for(int i=0;i<3;i++) {
                Rectangle r=new Rectangle(i*w+4,4,w-12,Height-12);
                Theme.Fill(g,r,i==0?Color.FromArgb(239,235,254):Theme.Canvas,12);
                using(var f=Theme.Font(9))Theme.Text(g,names[i],f,Theme.Muted,new Rectangle(r.X+18,r.Y+12,r.Width-28,20));
                using(var f=Theme.Font(23,true))Theme.Text(g,values[i],f,i==0?Theme.Purple:Theme.Ink,new Rectangle(r.X+16,r.Y+37,r.Width-26,45));
            }
        }
    }
    public class ModernButton:Button  {
        bool hover;
        public bool Primary;
        public ModernButton() {
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            FlatStyle=FlatStyle.Flat;
            FlatAppearance.BorderSize=0;
            Cursor=Cursors.Hand;
            Font=Theme.Font(9);
            BackColor=Color.White;
            Padding=new Padding(10,0,10,0);
            MouseEnter+=(o,e)=> {
                hover=true;
                Invalidate();
            }
            ;
            MouseLeave+=(o,e)=> {
                hover=false;
                Invalidate();
            }
            ;
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent==null?Theme.Canvas:Parent.BackColor);
            Color bg=Primary?(hover?Color.FromArgb(95,76,203):Theme.Purple):(hover?Color.FromArgb(235,231,253):Color.FromArgb(242,242,249));
            Theme.Fill(e.Graphics,new Rectangle(0,0,Width-1,Height-1),bg,9);
            Theme.Text(e.Graphics,Text,Font,Primary?Color.White:Theme.Ink,ClientRectangle,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            if(Focused) {
                using(var pen=new Pen(Primary?Color.White:Theme.Purple)) {
                    pen.DashStyle=DashStyle.Dot;
                    using(var path=Theme.Round(new Rectangle(4,4,Width-9,Height-9),6))e.Graphics.DrawPath(pen,path);
                }
            }
        }
    }
    public class CardHeader:Control  {
        public int Done, Total;
        public string Subtitle="";
        public CardHeader() {
            DoubleBuffered=true;
            Height=184;
            BackColor=Theme.Canvas;
        }
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            Theme.Fill(g,new Rectangle(10,10,Width-20,Height-20),Color.FromArgb(44,43,70),18);
            var clip=g.Save();
            using(var path=Theme.Round(new Rectangle(10,10,Width-20,Height-20),18))g.SetClip(path);
            using(var b=new SolidBrush(Color.FromArgb(57,55,88)))g.FillEllipse(b,Width-119,-35,180,180);
            g.Restore(clip);
            Theme.Fill(g,new Rectangle(Width-55,24,28,25),Color.FromArgb(78,72,111),8);
            using(var pen=new Pen(Color.FromArgb(215,208,240),2))g.DrawLine(pen,Width-47,36,Width-35,36);
            using(var f=Theme.Font(9))Theme.Text(g,"FOCUS  /  每日一点进步",f,Color.FromArgb(190,182,227),new Rectangle(29,25,Width-95,20));
            using(var f=Theme.Font(21,true))Theme.Text(g,"今日任务",f,Color.White,new Rectangle(27,49,250,42));
            using(var f=Theme.Font(8.5f))Theme.Text(g,Subtitle,f,Color.FromArgb(191,190,211),new Rectangle(29,94,Width-55,24));
            using(var f=Theme.Font(10,true))Theme.Text(g,Done+" / "+Total+"  已完成",f,Color.White,new Rectangle(29,129,190,24));
            using(var f=Theme.Font(10))Theme.Text(g,Total==0?"待启程":((int)(100.0*Done/Total))+"%",f,Color.FromArgb(194,180,255),new Rectangle(Width-89,129,60,24),TextFormatFlags.Right|TextFormatFlags.VerticalCenter);
            Rectangle track=new Rectangle(29,159,Width-58,4);
            Theme.Fill(g,track,Color.FromArgb(78,73,109),2);
            if(Total>0&&Done>0)Theme.Fill(g,new Rectangle(track.X,track.Y,Math.Max(4,track.Width*Done/Total),4),Color.FromArgb(175,154,255),2);
        }
    }
    public class TaskList:ListBox  {
        public TaskList() {
            DrawMode=DrawMode.OwnerDrawFixed;
            ItemHeight=108;
            BorderStyle=BorderStyle.None;
            BackColor=Theme.Canvas;
            IntegralHeight=false;
            Font=Theme.Font(10);
        }
        protected override void OnDrawItem(DrawItemEventArgs e) {
            if(e.Index<0)return;
            TaskGroup item=(TaskGroup)Items[e.Index];
            var g=e.Graphics;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            bool done=item.Completed==item.Total,selected=(e.State&DrawItemState.Selected)!=0;
            using(var b=new SolidBrush(Theme.Canvas))g.FillRectangle(b,e.Bounds);
            Rectangle box=new Rectangle(e.Bounds.X+4,e.Bounds.Y+3,e.Bounds.Width-10,e.Bounds.Height-8);
            Theme.Fill(g,box,selected?Color.FromArgb(236,232,253):Color.White,12);
            Rectangle check=new Rectangle(box.X+14,box.Y+22,20,20);
            Theme.Fill(g,check,done?Theme.Mint:Color.FromArgb(243,243,250),6);
            if(done) {
                using(var p=new Pen(Color.White,2)) {
                    p.StartCap=p.EndCap=LineCap.Round;
                    g.DrawLines(p,new Point[] {
                        new Point(check.X+5,check.Y+10),new Point(check.X+9,check.Y+14),new Point(check.X+16,check.Y+6)
                    }
                    );
                }
            }
            else using(var p=new Pen(Color.FromArgb(209,207,226)))using(var path=Theme.Round(check,6))g.DrawPath(p,path);
            using(var f=Theme.Font(10,true))Theme.Text(g,item.Title,f,done?Theme.Muted:Theme.Ink,new Rectangle(box.X+46,box.Y+9,box.Width-62,25));
            Color accent=item.Kind=="复习"?Theme.Purple:item.Kind=="附加"?Color.FromArgb(183,125,39):Theme.Muted;
            string sub=item.Kind+"  ·  已完成 "+item.Completed+" / "+item.Total+" 份   › 修改";
            using(var f=Theme.Font(8))Theme.Text(g,"今日新增 "+item.NewToday(DateTime.Today)+" · 往日结转 "+item.Carried(DateTime.Today),f,Theme.Muted,new Rectangle(box.X+46,box.Y+56,box.Width-60,20));
            Rectangle track=new Rectangle(box.X+46,box.Y+85,box.Width-64,4);
            Theme.Fill(g,track,Theme.Line,2);
            if(item.Completed>0)Theme.Fill(g,new Rectangle(track.X,track.Y,Math.Max(4,track.Width*item.Completed/item.Total),4),done?Theme.Mint:Theme.Purple,2);
            using(var f=Theme.Font(8))Theme.Text(g,sub,f,accent,new Rectangle(box.X+46,box.Y+35,box.Width-60,20));
        }
    }
}
