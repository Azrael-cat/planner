using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.Threading;
namespace DeskTasks  {
    public partial class Manager:Form  {
        Card card;
        ListView projects=new ListView();
        ListView all=new ListView();
        TabControl tabs=new TabControl();
        public Manager(Card c) {
            card=c;
            Text="任务中心 · 桌面任务小助手";
            Font=new Font("Microsoft YaHei UI",10);
            ClientSize=new Size(1000,770);
            MinimumSize=new Size(860,640);
            StartPosition=FormStartPosition.CenterParent;
            tabs.Dock=DockStyle.Fill;
            Controls.Add(tabs);
            BuildProjects();
            BuildAll();
            BuildStats();
            BuildSettings();
            Reload();
            EventHandler dayChanged=(sender,args)=>Reload();
            card.DayChanged+=dayChanged;
            FormClosed+=(sender,args)=>card.DayChanged-=dayChanged;
            Theme.Apply(this);
            var masthead=new Label {
                Dock=DockStyle.Top,Height=66,Text="任务中心   /   把进步留在每一天",Font=Theme.Font(17,true),ForeColor=Theme.Ink,Padding=new Padding(22,16,0,0)
            }
            ;
            Controls.Add(masthead);
        }
        TabPage Page(string title) {
            var p=new TabPage(title) {
                BackColor=Color.White,Padding=new Padding(14)
            }
            ;
            tabs.TabPages.Add(p);
            return p;
        }
        void BuildProjects() {
            var page=Page("总任务");
            projects.Dock=DockStyle.Fill;
            projects.View=View.Details;
            projects.FullRowSelect=true;
            projects.Columns.Add("任务",300);
            projects.Columns.Add("已完成 / 总量",140);
            projects.Columns.Add("截止日期",120);
            projects.Columns.Add("复习间隔",100);
            projects.Columns.Add("状态",85);
            var bar=new FlowLayoutPanel {
                Dock=DockStyle.Bottom,Height=55
            }
            ;
            bar.Controls.Add(Card.Button("添加总任务",()=> {
                using(var f=new ProjectForm(null))if(f.ShowDialog(this)==DialogResult.OK) {
                    card.Data.Projects.Add(f.Result);Replan();
                }
            }
            ));
            bar.Controls.Add(Card.Button("编辑选中任务",()=> {
                Project p=SelectedProject();if(p==null)return;using(var f=new ProjectForm(p))if(f.ShowDialog(this)==DialogResult.OK) {
                    card.Data.Projects[card.Data.Projects.IndexOf(p)]=f.Result;foreach(Item i in card.Data.Items.Where(i=>i.ProjectId==p.Id))i.Title=f.Result.Title;Replan();
                }
            }
            ));
            bar.Controls.Add(Card.Button("归档 / 恢复",()=> {
                Project p=SelectedProject();if(p==null)return;p.Archived=!p.Archived;Replan();
            }
            ));
            bar.Controls.Add(Card.Button("删除任务",()=> {
                Project p=SelectedProject();if(p==null)return;if(MessageBox.Show(this,"删除“"+p.Title+"”及其完成记录、复习记录？此操作不能撤销。建议使用归档保留统计。","确认删除",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes) {
                    card.Data.Projects.Remove(p);card.Data.Items.RemoveAll(i=>i.ProjectId==p.Id);Replan();
                }
            }
            ));
            page.Controls.Add(projects);
            page.Controls.Add(bar);
        }
        Project SelectedProject() {
            if(projects.SelectedItems.Count==0) {
                MessageBox.Show(this,"请先选中一条总任务。");
                return null;
            }
            return (Project)projects.SelectedItems[0].Tag;
        }
        void Replan() {
            Engine.Plan(card.Data,System.DateTime.Today,true);
            card.Save();
            Reload();
            card.RefreshCard();
        }
        void Reload() {
            projects.Items.Clear();
            foreach(Project p in card.Data.Projects) {
                int done=card.Data.Items.Count(i=>i.ProjectId==p.Id&&i.Kind=="任务"&&i.Done!="");
                var r=new ListViewItem(p.Title);
                r.SubItems.Add(done+" / "+p.Quantity);
                r.SubItems.Add(p.Deadline);
                r.SubItems.Add(p.ReviewDays==0?"不复习":p.ReviewDays+" 天");
                r.SubItems.Add(p.Archived?"已归档":done==p.Quantity?"已完成":"进行中");
                r.Tag=p;
                projects.Items.Add(r);
            }
            ReloadAll();
            UpdateStats();
        }
        void BuildSettings() {
            var page=Page("工作设置与备份");
            page.AutoScroll=true;
            var p=new TableLayoutPanel {
                Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(6)
            }
            ;
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,175));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,590));
            page.Controls.Add(p);
            var begin=TimePicker(card.Data.Settings.WorkStart);
            var end=TimePicker(card.Data.Settings.WorkEnd);
            var summary=TimePicker(card.Data.Settings.SummaryTime);
            ProjectForm.Add(p,"每日工作开始",begin,0);
            ProjectForm.Add(p,"每日工作结束",end,1);
            ProjectForm.Add(p,"每日总结提醒",summary,2);
            var daysPanel=new FlowLayoutPanel {
                AutoSize=true,Dock=DockStyle.Fill
            }
            ;
            var weights=new NumericUpDown[7];
            string[] names= {
                "日","一","二","三","四","五","六"
            }
            ;
            for(int n=0;n<7;n++) {
                var col=new FlowLayoutPanel {
                    FlowDirection=FlowDirection.TopDown,Width=70,Height=70
                }
                ;
                col.Controls.Add(new Label {
                    Text="周"+names[n],AutoSize=true
                }
                );
                weights[n]=new NumericUpDown {
                    Minimum=0,Maximum=100,Value=card.Data.Settings.Weights[n],Width=61
                }
                ;
                col.Controls.Add(weights[n]);
                daysPanel.Controls.Add(col);
            }
            p.Controls.Add(new Label {
                Text="每周每日配额权重",AutoSize=true
            }
            ,0,3);
            p.Controls.Add(daysPanel,1,3);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
            var preset=new FlowLayoutPanel {
                AutoSize=true
            }
            ;
            preset.Controls.Add(Card.Button("每周 5 天",()=> {
                for(int n=0;n<7;n++)weights[n].Value=n==0||n==6?0:20;
            }
            ));
            preset.Controls.Add(Card.Button("每周 6 天",()=> {
                for(int n=0;n<7;n++)weights[n].Value=n==0?0:1;
            }
            ));
            preset.Controls.Add(Card.Button("每周 7 天",()=> {
                for(int n=0;n<7;n++)weights[n].Value=1;
            }
            ));
            p.Controls.Add(preset,1,4);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
            var tip=new Label {
                Text="0 = 休息。权重自动归一化：20/20/20/20/20 即每天 20%。\n剩余任务按可工作日权重分摊，取整可能产生少量差异。\n若截止日前无工作日，剩余任务安排在截止日；过期任务安排在今天。\n工作时长用于效率估算；每日分配数量由配额权重决定。",AutoSize=true
            }
            ;
            p.Controls.Add(tip,1,5);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,95));
            var top=new CheckBox {
                Text="桌面卡片始终置顶",Checked=card.Data.Settings.Topmost,AutoSize=true
            }
            ;
            p.Controls.Add(top,1,6);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,32));
            p.Controls.Add(Card.Button("保存工作设置",()=> {
                if(weights.Sum(w=>(int)w.Value)==0) {
                    MessageBox.Show(this,"每周至少安排一个工作日。");return;
                }
                if(end.Value.TimeOfDay<=begin.Value.TimeOfDay) {
                    MessageBox.Show(this,"工作结束时间须晚于开始时间；暂不支持跨午夜班次。");return;
                }
                card.Data.Settings.WorkStart=begin.Value.ToString("HH:mm");card.Data.Settings.WorkEnd=end.Value.ToString("HH:mm");card.Data.Settings.SummaryTime=summary.Value.ToString("HH:mm");card.Data.Settings.Weights=weights.Select(w=>(int)w.Value).ToArray();card.Data.Settings.Topmost=top.Checked;Replan();MessageBox.Show(this,"已保存，未来安排已更新；今日及积压数量保留。");
            }
            ),1,7);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
            var bar=new FlowLayoutPanel {
                AutoSize=true
            }
            ;
            bar.Controls.Add(Card.Button("导出备份",()=> {
                using(var d=new SaveFileDialog {
                    Filter="JSON 备份|*.json",FileName="任务备份-"+Engine.Key(System.DateTime.Today)+".json"
                }
                )if(d.ShowDialog(this)==DialogResult.OK) {
                    File.WriteAllText(d.FileName,Store.Json.Serialize(card.Data),Encoding.UTF8);MessageBox.Show(this,"备份已导出。");
                }
            }
            ));
            bar.Controls.Add(Card.Button("导入备份",()=> {
                using(var d=new OpenFileDialog {
                    Filter="JSON 备份|*.json"
                }
                )if(d.ShowDialog(this)==DialogResult.OK) {
                    try {
                        State next=Store.Read(d.FileName);if(MessageBox.Show(this,"导入将替换当前全部数据。是否继续？","恢复备份",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;File.Copy(Store.FilePath,Path.Combine(Store.Root,"before-import-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json"),true);card.Data=next;Replan();MessageBox.Show(this,"已恢复。原数据已留存导入前备份；重新打开任务中心可查看恢复后的设置。");Close();
                    }
                    catch(Exception ex) {
                        MessageBox.Show(this,"无法导入："+ex.Message);
                    }
                }
            }
            ));
            p.Controls.Add(bar,1,8);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
            var startup=new CheckBox {
                Text="登录 Windows 时自动启动",AutoSize=true,Checked=File.Exists(Program.StartupPath)
            }
            ;
            startup.CheckedChanged+=(o,e)=> {
                try {
                    Program.SetStartup(startup.Checked);
                }
                catch(Exception ex) {
                    MessageBox.Show(this,"无法更改自启动："+ex.Message);
                }
            }
            ;
            p.Controls.Add(startup,1,9);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,32));
            var uninstall=Card.Button("卸载小助手",()=> {
                string script=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Uninstall.ps1");if(!File.Exists(script)) {
                    MessageBox.Show(this,"请使用安装目录内的卸载脚本。");return;
                }
                if(MessageBox.Show(this,"退出并卸载程序？卸载时可选择是否保留任务数据。","卸载",MessageBoxButtons.YesNo)==DialogResult.Yes) {
                    Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+script+"\"") {
                        UseShellExecute=false,CreateNoWindow=true
                    }
                    );Application.Exit();
                }
            }
            );
            p.Controls.Add(uninstall,1,10);
            p.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
            p.Controls.Add(new Label {
                Text="数据保存在本机；关闭卡片会保留悬浮入口，托盘菜单可退出。\n电脑关机或程序退出期间不弹提醒，下次启动会更新今日任务。",AutoSize=true
            }
            ,1,11);
        }
        DateTimePicker TimePicker(string t) {
            return new DateTimePicker {
                Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm",ShowUpDown=true,Value=System.DateTime.Today.Add(TimeSpan.Parse(t))
            }
            ;
        }
    }
}
