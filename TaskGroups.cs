using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace DeskTasks {
 public class TaskGroup {
  public string Key;
  public List<Item> Items;
  public string Title {get{return Items[0].Title;}}
  public string Kind {get{return Items[0].Kind;}}
  public int Total {get{return Items.Count;}}
  public int Completed {get{return Items.Count(i=>i.Done!="");}}
  public int Carried(DateTime day){return Items.Count(i=>String.CompareOrdinal(i.Due,Engine.Key(day))<0);}
  public int NewToday(DateTime day){return Items.Count(i=>i.Due==Engine.Key(day));}
  public override string ToString(){return Title+"，"+Kind+"，今日已完成 "+Completed+" / "+Total+" 份";}
 }
 public static class TaskGroups {
  public static List<TaskGroup> Today(State state,DateTime date){return Engine.Today(state,date).GroupBy(i=>String.IsNullOrEmpty(i.ProjectId)?Store.Json.Serialize(new string[]{"附加",i.Title,i.Due,i.Origin}):i.ProjectId+":"+i.Kind).Select(g=>new TaskGroup{Key=g.Key,Items=g.ToList()}).OrderBy(g=>g.Completed==g.Total).ThenBy(g=>g.Items.Min(i=>i.Due)).ThenBy(g=>g.Key).ToList();}
  public static void SetCompleted(State state,TaskGroup group,int target,DateTime date){
   if(target<0||target>group.Total)throw new ArgumentOutOfRangeException("target","完成数量应在 0 到今日计划数量之间。");
   var todayIds=new HashSet<string>(Engine.Today(state,date).Select(i=>i.Id));
   if(group.Items.Any(i=>!todayIds.Contains(i.Id)))throw new InvalidOperationException("今日安排已变化，请重新打开任务后修改。");
   int delta=target-group.Completed;
   if(delta>0){foreach(Item item in group.Items.Where(i=>i.Done=="").OrderBy(i=>i.Due).ThenBy(i=>i.Number).Take(delta).ToList())Engine.Complete(state,item,date);}
   else if(delta<0){var reversible=group.Items.Where(i=>i.Done!=""&&!state.Items.Any(r=>r.Id==i.Id+":review"&&r.Done!="")).OrderByDescending(i=>i.Number).Take(-delta).ToList();if(reversible.Count < -delta)throw new InvalidOperationException("部分份数的复习已经完成，不能撤销对应原任务。请保留这些已完成份数。");foreach(Item item in reversible)Engine.Undo(state,item);}
  }
 }
 public class QuantityForm:Form {
  public QuantityForm(Card card,TaskGroup group){Text="更新完成数量";Font=Theme.Font(10);ClientSize=new Size(420,290);FormBorderStyle=FormBorderStyle.FixedDialog;StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;string day=Engine.Key(DateTime.Today);
   var title=new Label{Text=group.Title,Font=Theme.Font(15,true),Location=new Point(22,20),Size=new Size(375,36),AutoEllipsis=true};
   var description=new Label{Text=group.Kind+" · 今日计划 "+group.Total+" 份",Location=new Point(24,63),AutoSize=true};
   var label=new Label{Text="今日已完成",Location=new Point(24,107),AutoSize=true};
   var number=new NumericUpDown{Minimum=0,Maximum=group.Total,Value=group.Completed,Location=new Point(150,100),Size=new Size(240,34),Font=Theme.Font(15),Name="completedQuantity"};
   var quick=new FlowLayoutPanel{Location=new Point(145,145),Size=new Size(250,45)};quick.Controls.Add(Card.Button("＋ 1 份",()=>{if(number.Value<number.Maximum)number.Value++;}));quick.Controls.Add(Card.Button("全部完成",()=>number.Value=number.Maximum));
   var help=new Label{Text="输入的是累计完成数量；减少数量可撤销完成。",Location=new Point(24,201),Size=new Size(380,25),Font=Theme.Font(8)};
   var save=Card.Button("保存数量",()=>{if(day!=Engine.Key(DateTime.Today)){MessageBox.Show(this,"日期已变化，请重新打开今日任务。");DialogResult=DialogResult.Cancel;return;}try{TaskGroups.SetCompleted(card.Data,group,(int)number.Value,DateTime.Today);card.Save();DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,ex.Message,"无法更新数量");}});save.Location=new Point(275,239);save.Name="saveQuantity";
   Controls.AddRange(new Control[]{title,description,label,number,quick,help,save});Theme.Apply(this);AcceptButton=save;Shown+=(o,e)=>{number.Focus();number.Select(0,number.Text.Length);};
  }
 }
 public static class GroupTests {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  public static void Run(){var day=new DateTime(2026,10,12);var s=new State();var p=new Project{Title="同一任务",Quantity=120,Start=Engine.Key(day),Deadline=Engine.Key(day),ReviewDays=7};s.Projects.Add(p);Engine.Plan(s,day);var g=TaskGroups.Today(s,day).Single();Check(g.Total==120&&g.Completed==0,"120 units must form one row");TaskGroups.SetCompleted(s,g,35,day);Check(g.Completed==35&&s.Items.Count(i=>i.Kind=="复习")==35,"batch completion creates reviews");TaskGroups.SetCompleted(s,g,35,day);Check(s.Items.Count(i=>i.Kind=="复习")==35,"same count is idempotent");TaskGroups.SetCompleted(s,g,20,day);Check(g.Completed==20&&s.Items.Count(i=>i.Kind=="复习")==20,"decrease removes only undone reviews");var review=s.Items.First(i=>i.Kind=="复习");review.Done=Engine.Key(day);bool rejected=false;try{TaskGroups.SetCompleted(s,g,0,day);}catch(InvalidOperationException){rejected=true;}Check(rejected&&g.Completed==20,"locked undo is atomic");TaskGroups.SetCompleted(s,g,1,day);Check(g.Completed==1,"reversible units can be reduced");Check(TaskGroups.Today(s,day).Count==2,"review and task remain separate");var p2=new Project{Title=p.Title,Quantity=2,Start=p.Start,Deadline=p.Deadline};s.Projects.Add(p2);Engine.Plan(s,day);Check(TaskGroups.Today(s,day).Count(x=>x.Kind=="任务")==2,"same name projects remain distinct");s.Items.Add(new Item{Id="a",Title="附加",Kind="附加",Due=Engine.Key(day),Origin="总结"});s.Items.Add(new Item{Id="b",Title="附加",Kind="附加",Due=Engine.Key(day),Origin="总结"});Check(TaskGroups.Today(s,day).Single(x=>x.Kind=="附加").Total==2,"matching extras group");bool range=false;try{TaskGroups.SetCompleted(s,g,121,day);}catch(ArgumentOutOfRangeException){range=true;}Check(range&&g.Completed==1,"invalid quantity leaves state intact");}
 }
}
