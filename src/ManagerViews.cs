using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace DeskTasks  {
    public class ScheduleGroup  {
        public Project Project;
        public List<Item> Items;
        public string Title  {
            get {
                return Project==null?Items[0].Title:Project.Title;
            }
        }
        public static List<ScheduleGroup> All(State state) {
            var result=state.Projects.Select(p=>new ScheduleGroup {
                Project=p,Items=state.Items.Where(i=>i.ProjectId==p.Id).ToList()
            }
            ).ToList();
            result.AddRange(state.Items.Where(i=>String.IsNullOrEmpty(i.ProjectId)).GroupBy(i=>Store.Json.Serialize(new string[] {
                i.Title,i.Kind,i.Due,i.Origin
            }
            )).Select(g=>new ScheduleGroup {
                Items=g.ToList()
            }
            ));
            return result;
        }
    }
    public class ProjectReport  {
        public Project Project;
        public int Done,TodayDone,TodayTotal,ReviewDone,ReviewTotal;
        public double Speed;
        public int[] Daily=new int[14],Reviews=new int[14];
        public DateTime Date;
        public static ProjectReport Build(State state,Project project,DateTime date) {
            var report=new ProjectReport {
                Project=project,Date=date
            }
            ;
            var items=state.Items.Where(i=>i.ProjectId==project.Id).ToList();
            var today=items.Where(i=>i.Done==Engine.Key(date)||(i.Done==""&&String.CompareOrdinal(i.Due,Engine.Key(date))<=0)).ToList();
            report.Done=items.Count(i=>i.Kind=="任务"&&i.Done!="");
            report.TodayDone=today.Count(i=>i.Done!="");
            report.TodayTotal=today.Count;
            report.ReviewDone=items.Count(i=>i.Kind=="复习"&&i.Done!="");
            report.ReviewTotal=items.Count(i=>i.Kind=="复习");
            TimeSpan a,b;
            if(TimeSpan.TryParse(state.Settings.WorkStart,out a)&&TimeSpan.TryParse(state.Settings.WorkEnd,out b)&&(b-a).TotalHours>0)report.Speed=report.TodayDone/(b-a).TotalHours;
            for(int n=0;n<14;n++) {
                string d=Engine.Key(date.AddDays(n-13));
                report.Daily[n]=items.Count(i=>i.Done==d&&i.Kind=="任务");
                report.Reviews[n]=items.Count(i=>i.Done==d&&i.Kind=="复习");
            }
            return report;
        }
    }
    public class ProjectReportCard:Control  {
        public ProjectReport Report;
        ToolTip tip=new ToolTip();
        public ProjectReportCard(ProjectReport report) {
            Report=report;
            Height=218;
            Margin=new Padding(3,5,3,9);
            DoubleBuffered=true;
            BackColor=Theme.Canvas;
            AccessibleName=report.Project.Title+"的进度与效率";
        }
        protected override void Dispose(bool disposing) {
            if(disposing)tip.Dispose();
            base.Dispose(disposing);
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            int x=Math.Max(320,(int)(Width*.51))+15,w=Width-x-25;
            string text="";
            if(w>0&&e.X>=x&&e.X<x+w&&e.Y>=70&&e.Y<=175) {
                int n=Math.Min(13,(int)((e.X-x)*14.0/w));
                text=Report.Date.AddDays(n-13).ToString("yyyy-MM-dd")+"：任务 "+Report.Daily[n]+" 份，复习 "+Report.Reviews[n]+" 份";
            }
            if(tip.GetToolTip(this)!=text)tip.SetToolTip(this,text);
        }
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            Theme.Fill(g,new Rectangle(0,0,Width-1,Height-1),Color.White,14);
            var r=Report;
            int leftWidth=Math.Max(320,(int)(Width*.51));
            using(var f=Theme.Font(12,true))Theme.Text(g,r.Project.Title+(r.Project.Archived?" · 已归档":""),f,Theme.Ink,new Rectangle(18,13,Width-40,30));
            double rate=r.Project.Quantity==0?0:100.0*r.Done/r.Project.Quantity;
            using(var f=Theme.Font(17,true))Theme.Text(g,r.Done+" / "+r.Project.Quantity+" 份  ·  "+rate.ToString("0.#")+"%",f,Theme.Purple,new Rectangle(18,54,leftWidth-40,36));
            Rectangle bar=new Rectangle(21,98,leftWidth-46,6);
            Theme.Fill(g,bar,Theme.Line,3);
            if(r.Done>0)Theme.Fill(g,new Rectangle(bar.X,bar.Y,Math.Max(3,(int)(bar.Width*rate/100)),6),Theme.Purple,3);
            using(var f=Theme.Font(9)) {
                Theme.Text(g,"今日完成  "+r.TodayDone+" / "+r.TodayTotal+" 份（含复习）",f,Theme.Ink,new Rectangle(19,115,leftWidth-30,23));
                Theme.Text(g,"效率  "+r.Speed.ToString("0.##")+" 份/计划小时",f,Theme.Muted,new Rectangle(19,143,leftWidth-30,23));
                Theme.Text(g,"复习完成 "+r.ReviewDone+" / "+r.ReviewTotal+" 份  ·  截止 "+r.Project.Deadline,f,Theme.Muted,new Rectangle(19,176,leftWidth-25,23));
            }
            int x=leftWidth+15,w=Width-x-25;
            using(var f=Theme.Font(8))Theme.Text(g,"近 14 天完成量   紫：任务  绿：复习",f,Theme.Muted,new Rectangle(x,48,w,23));
            int max=1;
            for(int n=0;n<14;n++)max=Math.Max(max,r.Daily[n]+r.Reviews[n]);
            int baseline=171;
            using(var pen=new Pen(Theme.Line))g.DrawLine(pen,x,baseline,x+w,baseline);
            float slot=w/14f;
            for(int n=0;n<14;n++) {
                int h=(int)(85.0*r.Daily[n]/max),rh=(int)(85.0*r.Reviews[n]/max);
                int bx=x+(int)(n*slot)+3,bw=Math.Max(3,(int)slot-7);
                if(h>0)Theme.Fill(g,new Rectangle(bx,baseline-h,bw,h),Theme.Purple,2);
                if(rh>0)Theme.Fill(g,new Rectangle(bx,baseline-h-rh,bw,rh),Theme.Mint,2);
            }
            using(var f=Theme.Font(8)) {
                Theme.Text(g,r.Date.AddDays(-13).ToString("MM-dd"),f,Theme.Muted,new Rectangle(x,179,60,20));
                Theme.Text(g,r.Date.ToString("MM-dd"),f,Theme.Muted,new Rectangle(x+w-60,179,60,20),TextFormatFlags.Right|TextFormatFlags.VerticalCenter);
                if(r.Daily.Sum()+r.Reviews.Sum()==0)Theme.Text(g,"暂无完成记录",f,Theme.Muted,new Rectangle(x,110,w,25),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
        }
    }
    public class ScheduleDetailForm:Form  {
        Card card;
        ScheduleGroup group;
        ListView list=new ListView();
        public ScheduleDetailForm(Card owner,ScheduleGroup selected) {
            card=owner;
            group=selected;
            Text=group.Title+" · 日期安排";
            Font=Theme.Font(10);
            ClientSize=new Size(760,490);
            StartPosition=FormStartPosition.CenterParent;
            list.Dock=DockStyle.Fill;
            list.View=View.Details;
            list.FullRowSelect=true;
            list.Columns.Add("计划日期",140);
            list.Columns.Add("类型",80);
            list.Columns.Add("已完成 / 总量",150);
            list.Columns.Add("来源",350);
            var heading=new Label {
                Dock=DockStyle.Top,Height=48,Text="按日期汇总；只调整或删除选中行中尚未完成的复习、附加任务。",Padding=new Padding(12,13,0,0)
            }
            ;
            var bar=new FlowLayoutPanel {
                Dock=DockStyle.Bottom,Height=55,Padding=new Padding(8)
            }
            ;
            bar.Controls.Add(Card.Button("调整未完成日期",()=>ChangeDate()));
            bar.Controls.Add(Card.Button("删除未完成安排",()=>DeletePending()));
            Controls.Add(list);
            Controls.Add(heading);
            Controls.Add(bar);
            Theme.Apply(this);
            Reload();
        }
        void Reload() {
            list.Items.Clear();
            foreach(var batch in group.Items.Where(i=>card.Data.Items.Contains(i)).GroupBy(i=>new {
                i.Due,i.Kind,i.Origin
            }
            ).OrderBy(g=>g.Key.Due).ThenBy(g=>g.Key.Kind)) {
                var items=batch.ToList();
                var row=new ListViewItem(batch.Key.Due);
                row.SubItems.Add(batch.Key.Kind);
                row.SubItems.Add(items.Count(i=>i.Done!="")+" / "+items.Count);
                row.SubItems.Add(batch.Key.Origin);
                row.Tag=items;
                list.Items.Add(row);
            }
        }
        List<Item> Pending() {
            if(list.SelectedItems.Count==0)return null;
            var items=(List<Item>)list.SelectedItems[0].Tag;
            if(items[0].Kind=="任务") {
                MessageBox.Show(this,"总任务的日期由工作配额分配，请在总任务页面调整。");
                return null;
            }
            var pending=items.Where(i=>i.Done=="").ToList();
            if(pending.Count==0) {
                MessageBox.Show(this,"这行安排已全部完成。");
                return null;
            }
            return pending;
        }
        void ChangeDate() {
            var items=Pending();
            if(items==null)return;
            using(var form=new Form {
                Text="调整 "+items.Count+" 份未完成安排",ClientSize=new Size(330,135),StartPosition=FormStartPosition.CenterParent,Font=Font
            }
            ) {
                var date=new DateTimePicker {
                    Location=new Point(20,20),Format=DateTimePickerFormat.Short,Value=Engine.Date(items[0].Due)
                }
                ;
                var save=Card.Button("保存日期",()=> {
                    foreach(var item in items)item.Due=Engine.Key(date.Value.Date);card.Save();form.DialogResult=DialogResult.OK;
                }
                );
                save.Location=new Point(20,70);
                form.Controls.Add(date);
                form.Controls.Add(save);
                Theme.Apply(form);
                form.ShowDialog(this);
            }
            Reload();
        }
        void DeletePending() {
            var items=Pending();
            if(items==null)return;
            if(MessageBox.Show(this,"删除选中日期的 "+items.Count+" 份未完成"+items[0].Kind+"？已完成记录保留。","确认删除",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            foreach(var item in items)card.Data.Items.Remove(item);
            card.Save();
            Reload();
        }
    }
    public partial class Manager  {
        StatsMetrics metrics;
        FlowLayoutPanel reports;
        TextBox history;
        void BuildAll() {
            var page=Page("全部安排");
            all.Name="groupedSchedules";
            all.Dock=DockStyle.Fill;
            all.View=View.Details;
            all.FullRowSelect=true;
            all.Columns.Add("总任务 / 附加任务",240);
            all.Columns.Add("已完成 / 总量",140);
            all.Columns.Add("总任务起止 / 附加日期",195);
            all.Columns.Add("今日已完成 / 计划",145);
            all.Columns.Add("复习已完成 / 已安排",150);
            all.Columns.Add("状态",90);
            all.DoubleClick+=(o,e)=>OpenSchedule();
            var bar=new FlowLayoutPanel {
                Dock=DockStyle.Bottom,Height=60,Padding=new Padding(4)
            }
            ;
            bar.Controls.Add(Card.Button("查看日期安排",()=>OpenSchedule()));
            bar.Controls.Add(new Label {
                Text="每个总任务仅一行；双击可按日期查看、管理复习和附加任务。",AutoSize=true,Margin=new Padding(12,13,0,0)
            }
            );
            page.Controls.Add(all);
            page.Controls.Add(bar);
        }
        void OpenSchedule() {
            if(all.SelectedItems.Count==0)return;
            using(var dialog=new ScheduleDetailForm(card,(ScheduleGroup)all.SelectedItems[0].Tag))dialog.ShowDialog(this);
            Reload();
            card.RefreshCard();
        }
        void ReloadAll() {
            all.BeginUpdate();
            all.Items.Clear();
            var today=new HashSet<string>(Engine.Today(card.Data,DateTime.Today).Select(i=>i.Id));
            foreach(var group in ScheduleGroup.All(card.Data)) {
                var original=group.Project==null?group.Items:group.Items.Where(i=>i.Kind=="任务").ToList();
                int done=original.Count(i=>i.Done!=""),total=group.Project==null?original.Count:group.Project.Quantity;
                var row=new ListViewItem(group.Title);
                row.SubItems.Add(done+" / "+total);
                string first=group.Project==null?group.Items.Min(i=>i.Due):group.Project.Start,last=group.Project==null?group.Items.Max(i=>i.Due):group.Project.Deadline;
                row.SubItems.Add(first==last?first:first+" ~ "+last);
                var due=group.Items.Where(i=>today.Contains(i.Id)).ToList();
                row.SubItems.Add(due.Count(i=>i.Done!="")+" / "+due.Count);
                var reviews=group.Items.Where(i=>i.Kind=="复习").ToList();
                row.SubItems.Add(reviews.Count(i=>i.Done!="")+" / "+reviews.Count);
                row.SubItems.Add(group.Project!=null&&group.Project.Archived?"已归档":done==total?"已完成":"进行中");
                row.Tag=group;
                all.Items.Add(row);
            }
            all.EndUpdate();
        }
        void BuildStats() {
            var page=Page("进度与效率");
            metrics=new StatsMetrics {
                Dock=DockStyle.Top,Height=118
            }
            ;
            reports=new FlowLayoutPanel {
                Name="projectReports",Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Theme.Canvas,Padding=new Padding(6)
            }
            ;
            reports.SizeChanged+=(o,e)=>FitReports();
            page.Controls.Add(reports);
            page.Controls.Add(metrics);
        }
        void FitReports() {
            foreach(Control control in reports.Controls)control.Width=Math.Max(650,reports.ClientSize.Width-34);
        }
        void UpdateStats() {
            if(reports==null)return;
            var s=card.Data;
            var rows=Engine.Today(s,DateTime.Today);
            int complete=rows.Count(i=>i.Done!="");
            TimeSpan a,b;
            double hours=TimeSpan.TryParse(s.Settings.WorkStart,out a)&&TimeSpan.TryParse(s.Settings.WorkEnd,out b)?(b-a).TotalHours:0;
            metrics.Progress=s.Items.Count(i=>i.Kind=="任务"&&i.Done!="")+" / "+s.Projects.Sum(p=>p.Quantity);
            metrics.Today=rows.Count==0?"—":(100.0*complete/rows.Count).ToString("0.#")+"%";
            metrics.Speed=(hours>0?complete/hours:0).ToString("0.##")+" 份";
            metrics.Invalidate();
            reports.SuspendLayout();
            foreach(Control c in reports.Controls.Cast<Control>().ToArray())c.Dispose();
            reports.Controls.Clear();
            var note=new Label {
                Text="每个总任务独立统计（含归档）。总进度不重复计入复习；效率按每日计划工时估算，并非实际计时。",Height=36,Margin=new Padding(6),ForeColor=Theme.Muted,Font=Theme.Font(8)
            }
            ;
            reports.Controls.Add(note);
            foreach(Project project in s.Projects)reports.Controls.Add(new ProjectReportCard(ProjectReport.Build(s,project,DateTime.Today)));
            if(s.Projects.Count==0)reports.Controls.Add(new Label {
                Text="还没有总任务。添加任务后，这里会分别显示每个目标的进度。",Height=70,ForeColor=Theme.Muted
            }
            );
            history=new TextBox {
                Height=150,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(4,8,4,12)
            }
            ;
            history.Text="每日总结历史\r\n\r\n"+String.Join("\r\n\r\n",s.Journals.OrderByDescending(j=>j.Date).Select(j=>j.Date+"\r\n"+j.Text));
            reports.Controls.Add(history);
            FitReports();
            reports.ResumeLayout();
        }
    }
}
