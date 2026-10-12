$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[void][Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'DeskTaskAssistant.exe'))
[System.Windows.Forms.Application]::EnableVisualStyles()
[DeskTasks.Store]::Root = Join-Path $PSScriptRoot 'qa-data'
$state = New-Object DeskTasks.State
$project = New-Object DeskTasks.Project
$project.Title = '英语词汇学习'
$project.Quantity = 100
$project.Start = [DeskTasks.Engine]::Key([DateTime]::Today)
$project.Deadline = [DeskTasks.Engine]::Key([DateTime]::Today.AddDays(14))
$project.ReviewDays = 7
$state.Projects.Add($project)
[DeskTasks.Engine]::Plan($state, [DateTime]::Today)
$card = New-Object DeskTasks.Card($state)
$card.Show()
[System.Windows.Forms.Application]::DoEvents()
$taskList = $card.Controls | ForEach-Object { $_.Controls } | Where-Object { $_ -is [DeskTasks.TaskList] }
if ($taskList.Items.Count -ne 1) { throw 'Total task was not grouped.' }
$taskList.SelectedIndex = 0
$selected = $taskList.SelectedItem
$quantity = [DeskTasks.QuantityForm]::new($card, $selected)
$quantity.Show()
[System.Windows.Forms.Application]::DoEvents()
$quantity.Controls['completedQuantity'].Value = 3
$quantity.Controls['saveQuantity'].PerformClick()
if ($selected.Completed -ne 3) { throw 'Quantity save failed.' }
$quantity.Dispose()
$card.RefreshCard()
$stream = [IO.File]::Create((Join-Path $PSScriptRoot 'App.ico'))
$icon = [DeskTasks.Theme]::MakeIcon()
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
function Capture($form, $name) {
    $bitmap = New-Object Drawing.Bitmap($form.Width, $form.Height)
    $form.DrawToBitmap($bitmap, (New-Object Drawing.Rectangle(0, 0, $form.Width, $form.Height)))
    $bitmap.Save((Join-Path $PSScriptRoot $name))
    $bitmap.Dispose()
}
Capture $card 'preview-card.png'
$quantity = [DeskTasks.QuantityForm]::new($card, $selected)
$quantity.Show()
[System.Windows.Forms.Application]::DoEvents()
Capture $quantity 'preview-quantity.png'
$quantity.Dispose()
$card.Collapse()
[System.Windows.Forms.Application]::DoEvents()
if ($card.Visible) { throw 'Collapse did not hide the card.' }
$mini = [System.Windows.Forms.Application]::OpenForms | Where-Object { $_.Text -eq '展开任务' }
if (-not $mini.Visible) { throw 'Restore button is missing.' }
Capture $mini 'preview-collapsed.png'
$mini.Controls[0].PerformClick()
[System.Windows.Forms.Application]::DoEvents()
if (-not $card.Visible) { throw 'Restore button failed.' }
$secondProject = New-Object DeskTasks.Project
$secondProject.Title = '算法练习与复盘'
$secondProject.Quantity = 160
$secondProject.Start = $project.Start
$secondProject.Deadline = $project.Deadline
$state.Projects.Add($secondProject)
[DeskTasks.Engine]::Plan($state, [DateTime]::Today, $false)
$manager = New-Object DeskTasks.Manager($card)
$manager.Show()
$tabs = $manager.Controls[0]
for ($index = 0; $index -lt $tabs.TabPages.Count; $index++) {
    $tabs.SelectedIndex = $index
    [System.Windows.Forms.Application]::DoEvents()
    Capture $manager ('preview-center-' + $index + '.png')
}
$allList = $tabs.TabPages[1].Controls['groupedSchedules']
if ($allList.Items.Count -ne 2) { throw 'All schedules must have one row per total task, including its reviews.' }
$reportCards = @($tabs.TabPages[2].Controls['projectReports'].Controls | Where-Object { $_ -is [DeskTasks.ProjectReportCard] })
if ($reportCards.Count -ne 2) { throw 'Every project must have its own statistics card.' }
$detail = [DeskTasks.ScheduleDetailForm]::new($card, $allList.Items[0].Tag)
$detail.Show()
[System.Windows.Forms.Application]::DoEvents()
Capture $detail 'preview-schedule-detail.png'
$detail.Dispose()
$editor = [DeskTasks.ProjectForm]::new($null)
$editor.Show()
[System.Windows.Forms.Application]::DoEvents()
Capture $editor 'preview-editor.png'
$summary = New-Object DeskTasks.SummaryForm($card)
$summary.Show()
[System.Windows.Forms.Application]::DoEvents()
Capture $summary 'preview-summary.png'
$summary.Dispose()
$editor.Dispose()
$manager.Dispose()
$card.Dispose()
Write-Output 'UI smoke check passed: card, four task-center tabs, editor, and summary rendered.'
