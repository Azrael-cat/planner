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
    public class ProjectForm:Form  {
        public Project Result;
        TextBox title=new TextBox();
        NumericUpDown quantity=new NumericUpDown();
        DateTimePicker start=new DateTimePicker(),end=new DateTimePicker();
        NumericUpDown review=new NumericUpDown();
        public ProjectForm(Project p) {
            Text=p==null?"添加总任务":"编辑总任务";
            Font=new Font("Microsoft YaHei UI",10);
            ClientSize=new Size(460,350);
            StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog;
            MaximizeBox=false;
            MinimizeBox=false;
            var grid=new TableLayoutPanel {
                Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=2,RowCount=7
            }
            ;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,125));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            title.MaxLength=160;
            quantity.Minimum=1;
            quantity.Maximum=10000;
            review.Maximum=3650;
            start.Format=end.Format=DateTimePickerFormat.Short;
            end.Value=System.DateTime.Today.AddDays(30);
            Add(grid,"任务内容",title,0);
            Add(grid,"总数量（份）",quantity,1);
            Add(grid,"开始日期",start,2);
            Add(grid,"完成截止日期",end,3);
            Add(grid,"几天后复习",review,4);
            var tip=new Label {
                Text="0 表示不复习；每份完成后安排一次复习。",AutoSize=true
            }
            ;
            grid.Controls.Add(tip,0,5);
            grid.SetColumnSpan(tip,2);
            var ok=Card.Button("保存任务",()=> {
                if(String.IsNullOrWhiteSpace(title.Text)) {
                    MessageBox.Show(this,"请填写任务内容。");return;
                }
                if(end.Value.Date<start.Value.Date) {
                    MessageBox.Show(this,"截止日期不能早于开始日期。");return;
                }
                if(p==null&&end.Value.Date<System.DateTime.Today) {
                    MessageBox.Show(this,"新任务的截止日期不能早于今天。");return;
                }
                Result=new Project {
                    Id=p==null?Guid.NewGuid().ToString("N"):p.Id,Title=title.Text.Trim(),Quantity=(int)quantity.Value,Start=Engine.Key(start.Value.Date),Deadline=Engine.Key(end.Value.Date),ReviewDays=(int)review.Value,Archived=p!=null&&p.Archived
                }
                ;DialogResult=DialogResult.OK;
            }
            );
            grid.Controls.Add(ok,1,6);
            Controls.Add(grid);
            if(p!=null) {
                title.Text=p.Title;
                quantity.Value=p.Quantity;
                quantity.Enabled=false;
                start.Value=Engine.Date(p.Start);
                end.Value=Engine.Date(p.Deadline);
                review.Value=p.ReviewDays;
                tip.Text="数量创建后固定；复习设置只影响此后完成的份数。";
            }
            Theme.Apply(this);
        }
        public static void Add(TableLayoutPanel panel,string label,Control control,int row) {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            panel.Controls.Add(new Label {
                Text=label,AutoSize=true,Anchor=AnchorStyles.Left
            }
            ,0,row);
            control.Dock=DockStyle.Fill;
            control.Margin=new Padding(3,7,3,7);
            panel.Controls.Add(control,1,row);
        }
    }
    public class SummaryForm:Form  {
        public SummaryForm(Card card) {
            Text="每日总结与未来附加任务";
            Font=new Font("Microsoft YaHei UI",10);
            ClientSize=new Size(590,510);
            StartPosition=FormStartPosition.CenterParent;
            var panel=new TableLayoutPanel {
                Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=8
            }
            ;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            string today=Engine.Key(System.DateTime.Today);
            Journal old=card.Data.Journals.FirstOrDefault(j=>j.Date==today);
            var note=new TextBox {
                Multiline=true,ScrollBars=ScrollBars.Vertical,Text=old==null?"":old.Text,MaxLength=12000
            }
            ;
            var task=new TextBox {
                MaxLength=160
            }
            ;
            var date=new DateTimePicker {
                Format=DateTimePickerFormat.Short,MinDate=System.DateTime.Today.AddDays(1),Value=System.DateTime.Today.AddDays(1)
            }
            ;
            var qty=new NumericUpDown {
                Minimum=1,Maximum=1000,Value=1
            }
            ;
            var info=new Label {
                Text="记录成果、阻碍和明天的调整。附加任务不占总任务数量。",Dock=DockStyle.Fill
            }
            ;
            panel.Controls.Add(info,0,0);
            panel.SetColumnSpan(info,2);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
            panel.Controls.Add(new Label {
                Text="今日总结",AutoSize=true
            }
            ,0,1);
            panel.Controls.Add(note,1,1);
            note.Dock=DockStyle.Fill;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute,175));
            ProjectForm.Add(panel,"附加任务",task,2);
            ProjectForm.Add(panel,"出现日期",date,3);
            ProjectForm.Add(panel,"附加数量",qty,4);
            var info2=new Label {
                Text="保存时新增附加任务；可多次添加不同日期。历史总结在统计页查看。",AutoSize=true
            }
            ;
            panel.Controls.Add(info2,0,5);
            panel.SetColumnSpan(info2,2);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            panel.Controls.Add(Card.Button("保存总结与附加任务",()=> {
                if(String.IsNullOrWhiteSpace(note.Text)&&String.IsNullOrWhiteSpace(task.Text)) {
                    MessageBox.Show(this,"请填写总结或附加任务。");return;
                }
                if(old==null) {
                    old=new Journal {
                        Date=today
                    }
                    ;card.Data.Journals.Add(old);
                }
                old.Text=note.Text.Trim();if(!String.IsNullOrWhiteSpace(task.Text))for(int n=1;n<=qty.Value;n++)card.Data.Items.Add(new Item {
                    Id=Guid.NewGuid().ToString("N"),Title=task.Text.Trim(),Kind="附加",Number=n,Due=Engine.Key(date.Value.Date),Origin="来自 "+today+" 的每日总结"
                }
                );card.Save();DialogResult=DialogResult.OK;
            }
            ),1,6);
            Controls.Add(panel);
            Theme.Apply(this);
        }
    }
}
