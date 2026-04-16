using System;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GmnPlayer.Models;
using GmnPlayer.Managers;
using System.Linq;

namespace GmnPlayer.UI
{
    public partial class LayoutEngine : Form
    {
        public enum EngineMode { Designer, Player }
        public EngineMode CurrentMode { get; private set; }

        private MenuStrip? _menuStrip;
        private ToolStrip? _toolStrip;
        private SplitContainer? _workspaceSplit;
        private SplitContainer? _canvasSplit;
        private TreeView? _treeView;
        private PropertyGrid? _propertyGrid;
        private Panel _canvasPanel = new Panel();
        private Button? _btnIdentify;

        // Interaction Engine
        private Control? _selectedControl;
        private List<Panel> _resizeHandles = new List<Panel>();
        private bool _isDragging = false;
        private Point _dragStartCursor;
        private Point _dragStartLocation;
        
        private bool _isResizing = false;
        private int _resizeHandleIndex = -1;
        private Rectangle _resizeStartBounds;

        // Undo & Productivity Engine
        private List<string> _undoStack = new List<string>();
        private List<WidgetModel> _clipboard = new List<WidgetModel>();

        public LayoutEngine(EngineMode mode = EngineMode.Player)
        {
            CurrentMode = mode;
            this.Text = CurrentMode == EngineMode.Designer ? "Gmn-Player v4.3.1 [Designer Workspace]" : "Gmn-Player v4.3.1 [Operator Dashboard]";
            this.Size = new Size(1366, 768);
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.KeyDown += LayoutEngine_KeyDown;

            InitializeUI();

            this.Load += (s, e) =>
            {
                // [BUG FIX 1] 빈 상태일 경우 확실하게 메모리 인스턴스를 만들고 전역(CurrentProject)에 장착함.
                if (ProjectManager.Instance.CurrentProject == null)
                {
                    var config = new ProjectConfig { ProjectId = "PRJ_SYS01" };
                    config.DeviceNodes.Add(new DeviceNode { DeviceId = "DEV_01", IpAddress = "127.0.0.1", Port = 5000, ProtocolStrategy = "AVCIT" });
                    config.Widgets.Add(new WidgetModel { Id = "grid_did_01", Type = WidgetType.GridCanvas, TargetDeviceId = "DEV_01", X = 200, Y = 100, W = 450, H = 300, ZIndex = 2, GridRows = 2, GridCols = 3 });
                    
                    ProjectManager.Instance.CurrentProject = config; // 핵심 결함 수리 완료
                    ProjectManager.Instance.SaveProject("default.gmn");
                }
                
                string json = JsonSerializer.Serialize(ProjectManager.Instance.CurrentProject);
                LoadLayoutFromJson(json);
                DeselectAll(); // Trigger ProjectConfig binding on init
                
                // Identify 통신 엔진 초기 구성 배선 (이제 CurrentProject가 무조건 있으므로 안전함)
                DeviceManager.Instance.InitializeFromConfig(ProjectManager.Instance.CurrentProject!);
            };
        }

        private void InitializeUI()
        {
            _canvasPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 25, 25) };
            _canvasPanel.MouseDown += (s, e) => DeselectAll();

            if (CurrentMode == EngineMode.Designer)
            {
                // [BUG FIX 4] 최상단 MenuStrip 강제 부활.
                _menuStrip = new MenuStrip { Dock = DockStyle.Top, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.LightGray };
                
                var fileMenu = new ToolStripMenuItem("File");
                fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save Project (.gmn)", null, (s,e) => { ProjectManager.Instance.SaveProject(); MessageBox.Show("Project Saved.", "Info"); }) { ShortcutKeys = Keys.Control | Keys.S });
                fileMenu.DropDownItems.Add(new ToolStripMenuItem("Exit", null, (s,e) => Application.Exit()));
                
                var editMenu = new ToolStripMenuItem("Edit");
                editMenu.DropDownItems.Add(new ToolStripMenuItem("Undo Action", null, (s,e) => PerformUndo()) { ShortcutKeys = Keys.Control | Keys.Z });
                
                _menuStrip.Items.Add(fileMenu); _menuStrip.Items.Add(editMenu);
                this.MainMenuStrip = _menuStrip;

                // ToolStrip 컴팩트 메뉴
                _toolStrip = new ToolStrip { Dock = DockStyle.Top, Height = 50, AutoSize = false, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(10, 5, 0, 5) };
                
                var lblAdd = new ToolStripLabel("Add Widget:");
                lblAdd.Font = new Font("Arial", 9, FontStyle.Bold);
                _toolStrip.Items.Add(lblAdd); _toolStrip.Items.Add(new ToolStripSeparator());

                foreach(WidgetType wt in Enum.GetValues(typeof(WidgetType)))
                {
                    var btn = new ToolStripButton($"  + {wt}  ") { DisplayStyle = ToolStripItemDisplayStyle.Text };
                    btn.Click += (s, e) => AddWidgetFromPalette(wt);
                    _toolStrip.Items.Add(btn);
                }

                _workspaceSplit = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 250, BackColor = Color.FromArgb(10, 10, 10) };
                _treeView = new TreeView {Dock = DockStyle.Fill, BackColor = Color.FromArgb(30,30,30), ForeColor = Color.LightGray, BorderStyle = BorderStyle.None, Font=new Font("Consolas", 10), HideSelection=false };
                _treeView.AfterSelect += TreeView_AfterSelect;

                _canvasSplit = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = this.Width - 300, BackColor = Color.FromArgb(10, 10, 10) };
                _propertyGrid = new PropertyGrid { Dock = DockStyle.Fill, PropertySort = PropertySort.Categorized, BackColor = Color.FromArgb(50,50,50), ToolbarVisible = false };
                _propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;

                Panel rightPanel = new Panel { Dock = DockStyle.Fill };
                _btnIdentify = new Button { Text = "Verify (Blink Hardware)", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.DarkRed, ForeColor=Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 9, FontStyle.Bold) };
                _btnIdentify.Click += BtnIdentify_Click;

                rightPanel.Controls.Add(_propertyGrid); rightPanel.Controls.Add(_btnIdentify);

                _canvasSplit.Panel1.Controls.Add(_canvasPanel); _canvasSplit.Panel2.Controls.Add(rightPanel);
                _workspaceSplit.Panel1.Controls.Add(_treeView); _workspaceSplit.Panel2.Controls.Add(_canvasSplit);

                // [BUG FIX 2] Dock 레이아웃 배치 붕괴 순서 재조정. 컨트롤 추가 순서에 따라 Z-Index가 정해지므로 역순 구성!
                this.Controls.Add(_workspaceSplit); // 1. 남는 공간 전부 차지
                this.Controls.Add(_toolStrip);      // 2. 상단 (MenuBar 아래)
                this.Controls.Add(_menuStrip);      // 3. 최상단 (제일 위에 렌더링)
                
                _workspaceSplit.BringToFront(); // 중앙으로 강제 푸시
                
                InitializeResizeHandles();
            }
            else
            {
                this.Controls.Add(_canvasPanel);
            }
        }

        private void PushUndoState()
        {
            var cfg = ProjectManager.Instance.CurrentProject;
            if (cfg != null)
            {
                _undoStack.Add(JsonSerializer.Serialize(cfg));
                if (_undoStack.Count > 20) _undoStack.RemoveAt(0);
            }
        }

        private void PerformUndo()
        {
            if (_undoStack.Count > 0)
            {
                string prevJson = _undoStack.Last();
                _undoStack.RemoveAt(_undoStack.Count - 1);
                
                LoadLayoutFromJson(prevJson);
                ProjectManager.Instance.SaveProject();
                DeselectAll();
            }
        }

        private int Snap(int value) => (int)Math.Round(value / 5.0) * 5;
        
        private void DeselectAll()
        {
            if (_selectedControl != null) _selectedControl.BackColor = _selectedControl.BackColor; // Dummy trigger
            _selectedControl = null;
            if (_propertyGrid != null) _propertyGrid.SelectedObject = ProjectManager.Instance.CurrentProject;
            if (_treeView != null) _treeView.SelectedNode = null;
            UpdateHandlesPosition();
        }

        private async void BtnIdentify_Click(object? sender, EventArgs e)
        {
            if (_propertyGrid?.SelectedObject is WidgetModel wm && !string.IsNullOrEmpty(wm.TargetDeviceId))
            {
                SystemLogger.Info(LogCategory.UI_EVENT, $"Designer Identify Device Triggered on: [{wm.TargetDeviceId}]");
                await DeviceManager.Instance.IdentifyDeviceAsync(wm.TargetDeviceId);
            }
            else
            {
                MessageBox.Show("Please select a valid DID Grid Widget or configure TargetDeviceId.", "Identify Blocked");
            }
        }

        private void PropertyGrid_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
        {
            PushUndoState();

            if (_propertyGrid?.SelectedObject is ProjectConfig prjConfig)
            {
                if (e.ChangedItem?.Label == "Background Image Path" && !string.IsNullOrEmpty(prjConfig.BackgroundImagePath))
                {
                    prjConfig.BackgroundImagePath = AssetManager.CopyAndGetRelativePath(prjConfig.BackgroundImagePath);
                }

                try
                {
                    _canvasPanel.BackColor = ColorTranslator.FromHtml(prjConfig.BackgroundColor);
                } catch { } // 무효 HTML 코드 방어
                
                string absBgPath = AssetManager.GetAbsolutePath(prjConfig.BackgroundImagePath);
                if (!string.IsNullOrEmpty(absBgPath) && System.IO.File.Exists(absBgPath)) {
                    _canvasPanel.BackgroundImage = Image.FromFile(absBgPath);
                    _canvasPanel.BackgroundImageLayout = ImageLayout.Stretch;
                } else {
                    _canvasPanel.BackgroundImage = null;
                }
            }
            else if (_propertyGrid?.SelectedObject is WidgetModel model)
            {
                if (e.ChangedItem?.Label == "Image Asset Path" && !string.IsNullOrEmpty(model.ImagePath))
                {
                    model.ImagePath = AssetManager.CopyAndGetRelativePath(model.ImagePath);
                }

                var targetControl = _canvasPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Name == model.Id);
                if (targetControl != null)
                {
                    // [BUG FIX 3] PropertyGrid에서 Row/Col 등을 변경했을 경우, 자식 셀 데이터가 갱신 안되는 문제 해결.
                    // 위젯을 폐기하고 즉시 재생성하여 캔버스에 다시 부착!
                    if (model.Type == WidgetType.GridCanvas || model.Type == WidgetType.SourceList)
                    {
                        var isSelected = _selectedControl == targetControl;
                        _canvasPanel.Controls.Remove(targetControl);
                        targetControl.Dispose();
                        
                        CreateAndPlaceControl(model);
                        var newControl = _canvasPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Name == model.Id);
                        if (newControl != null && isSelected) SelectControl(newControl, model);
                    }
                    else
                    {
                        UpdateControlVisuals(targetControl, model);
                    }
                    
                    UpdateHandlesPosition();
                    RefreshTreeViewNames();
                }
            }
            ProjectManager.Instance.SaveProject(); 
        }

        private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is WidgetModel model)
            {
                var targetControl = _canvasPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Name == model.Id);
                if (targetControl != null) SelectControl(targetControl, model);
            }
        }

        private void SelectControl(Control ctl, WidgetModel model)
        {
            _selectedControl = ctl;
            if (_propertyGrid != null) _propertyGrid.SelectedObject = model;
            
            if (_treeView != null)
            {
                foreach(TreeNode node in _treeView.Nodes)
                {
                    if (node.Tag == model) { _treeView.SelectedNode = node; break; }
                }
            }
            UpdateHandlesPosition();
        }

        private void AddWidgetFromPalette(WidgetType type)
        {
            var config = ProjectManager.Instance.CurrentProject;
            if (config != null)
            {
                PushUndoState();
                var newWidget = new WidgetModel { Type = type, X = 50, Y = 50, W = 150, H = 100, ZIndex=100, Id = "NEW_" + type.ToString() + "_" + Guid.NewGuid().ToString().Substring(0,4) };
                if(type == WidgetType.GridCanvas) { newWidget.W = 400; newWidget.H = 300; }
                
                config.Widgets.Add(newWidget);
                ProjectManager.Instance.SaveProject();
                
                CreateAndPlaceControl(newWidget);
                string alias = string.IsNullOrEmpty(newWidget.Command) ? newWidget.Id : newWidget.Command;
                if (_treeView != null) _treeView.Nodes.Add(new TreeNode($"[{newWidget.Type}] {alias}") { Tag = newWidget });
                
                var ctl = _canvasPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Name == newWidget.Id);
                if (ctl != null) SelectControl(ctl, newWidget);
            }
        }

        private void LayoutEngine_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.Alt && e.KeyCode == Keys.D)
            {
                MessageBox.Show("Maintenance Gate Activated.\nPlease restart the application to toggle modes if auto-start intercepts.", "Mode Switcher");
                return;
            }

            if (CurrentMode == EngineMode.Designer)
            {
                if (e.KeyCode == Keys.Escape) DeselectAll();
                
                // [Ctrl+C] Copy
                if (e.Control && e.KeyCode == Keys.C && _selectedControl != null)
                {
                    _clipboard.Clear();
                    var widget = ProjectManager.Instance.CurrentProject?.Widgets.FirstOrDefault(w => w.Id == _selectedControl.Name);
                    if (widget != null) {
                        string copyJson = JsonSerializer.Serialize(widget);
                        _clipboard.Add(JsonSerializer.Deserialize<WidgetModel>(copyJson)!);
                        SystemLogger.Info(LogCategory.UI_EVENT, $"Widget '{widget.Id}' copied to clipboard.");
                    }
                }
                
                // [Ctrl+V] Paste
                if (e.Control && e.KeyCode == Keys.V && _clipboard.Count > 0)
                {
                    PushUndoState();
                    var config = ProjectManager.Instance.CurrentProject;
                    foreach (var model in _clipboard)
                    {
                        var newClone = JsonSerializer.Deserialize<WidgetModel>(JsonSerializer.Serialize(model))!;
                        newClone.Id = "NEW_" + newClone.Type.ToString() + "_" + Guid.NewGuid().ToString().Substring(0, 4);
                        newClone.X += 20;
                        newClone.Y += 20;
                        config?.Widgets.Add(newClone);
                        CreateAndPlaceControl(newClone);
                        
                        string alias = string.IsNullOrEmpty(newClone.Command) ? newClone.Id : newClone.Command;
                        if (_treeView != null) _treeView.Nodes.Add(new TreeNode($"[{newClone.Type}] {alias}") { Tag = newClone });
                        
                        var ctl = _canvasPanel.Controls.OfType<Control>().FirstOrDefault(c => c.Name == newClone.Id);
                        if (ctl != null) SelectControl(ctl, newClone);
                    }
                    ProjectManager.Instance.SaveProject();
                }

                if (e.Control && e.KeyCode == Keys.Z) PerformUndo();
                if (e.KeyCode == Keys.Delete && _selectedControl != null)
                {
                    PushUndoState();
                    string id = _selectedControl.Name;
                    var widget = ProjectManager.Instance.CurrentProject?.Widgets.FirstOrDefault(w => w.Id == id);
                    if (widget != null) ProjectManager.Instance.CurrentProject?.Widgets.Remove(widget);
                    ProjectManager.Instance.SaveProject();
                    
                    _canvasPanel.Controls.Remove(_selectedControl);
                    _selectedControl.Dispose();
                    DeselectAll();
                    RefreshTreeNodesWhole();
                }
                
                if (e.Control && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) && _selectedControl != null)
                {
                    PushUndoState();
                    var widget = ProjectManager.Instance.CurrentProject?.Widgets.FirstOrDefault(w => w.Id == _selectedControl.Name);
                    if (widget != null)
                    {
                        if (e.KeyCode == Keys.Up) { widget.ZIndex += 1; _selectedControl.BringToFront(); }
                        if (e.KeyCode == Keys.Down) { widget.ZIndex -= 1; _selectedControl.SendToBack(); }
                        ProjectManager.Instance.SaveProject();
                        UpdateHandlesPosition();
                    }
                }
            }
        }

        private void RefreshTreeNodesWhole()
        {
            if (_treeView == null) return;
            _treeView.Nodes.Clear();
            var config = ProjectManager.Instance.CurrentProject;
            if (config != null)
            {
                var sorted = config.Widgets.OrderBy(w => w.ZIndex).ToList();
                foreach(var w in sorted)
                {
                    string alias = string.IsNullOrEmpty(w.Command) ? w.Id : w.Command;
                    _treeView.Nodes.Add(new TreeNode($"[{w.Type}] {alias}") { Tag = w });
                }
            }
        }

        private void LoadLayoutFromJson(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return;

            try
            {
                var config = JsonSerializer.Deserialize<ProjectConfig>(jsonContent);
                if (config != null)
                {
                    ProjectManager.Instance.CurrentProject = config; // BUG FIX: Synchronize the global state with the rendering context
                    ClearCanvas();
                    if (_treeView != null) _treeView.Nodes.Clear();

                    try { _canvasPanel.BackColor = ColorTranslator.FromHtml(config.BackgroundColor); } catch {}
                    string absBgPath = AssetManager.GetAbsolutePath(config.BackgroundImagePath);
                    if (!string.IsNullOrEmpty(absBgPath) && System.IO.File.Exists(absBgPath)) {
                        _canvasPanel.BackgroundImage = Image.FromFile(absBgPath);
                        _canvasPanel.BackgroundImageLayout = ImageLayout.Stretch;
                    }

                    var sortedWidgets = config.Widgets.OrderBy(w => w.ZIndex).ToList();
                    foreach (var widgetInfo in sortedWidgets)
                    {
                        CreateAndPlaceControl(widgetInfo);
                        if (_treeView != null)
                        {
                            string alias = string.IsNullOrEmpty(widgetInfo.Command) ? widgetInfo.Id : widgetInfo.Command;
                            _treeView.Nodes.Add(new TreeNode($"[{widgetInfo.Type}] {alias}") { Tag = widgetInfo });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SystemLogger.Error(LogCategory.SYSTEM, "Failed to parse Layout JSON.", ex);
            }
        }

        private void RefreshTreeViewNames()
        {
            if (_treeView == null) return;
            foreach(TreeNode node in _treeView.Nodes)
            {
                if (node.Tag is WidgetModel m)
                {
                    string alias = string.IsNullOrEmpty(m.Command) ? m.Id : m.Command;
                    node.Text = $"[{m.Type}] {alias}";
                }
            }
        }

        private void UpdateControlVisuals(Control c, WidgetModel model)
        {
            c.Left = Math.Max(0, model.X);
            c.Top = Math.Max(0, model.Y);
            c.Width = Math.Max(10, model.W);
            c.Height = Math.Max(10, model.H);

            if (c is Button btn && model.Type == WidgetType.Button) btn.Text = model.Command;
            if (c is Button layoutBtn && model.Type == WidgetType.LayoutChanger) layoutBtn.Text = model.Command;

            string absImgPath = AssetManager.GetAbsolutePath(model.ImagePath);
            if (!string.IsNullOrEmpty(absImgPath) && System.IO.File.Exists(absImgPath)) {
                c.BackgroundImage = Image.FromFile(absImgPath);
                c.BackgroundImageLayout = ImageLayout.Stretch;
            } else {
                c.BackgroundImage = null;
            }

            c.Invalidate();
            c.Update(); 
            _canvasPanel.Refresh(); 
        }

        private void CreateAndPlaceControl(WidgetModel ctlInfo)
        {
            Control panelControl;
            switch(ctlInfo.Type)
            {
                case WidgetType.GridCanvas: panelControl = CreateGridCanvas(ctlInfo); break;
                case WidgetType.SourceList: panelControl = CreateSourceList(ctlInfo); break;
                case WidgetType.LayoutChanger: panelControl = CreateLayoutChanger(ctlInfo); break;
                case WidgetType.PresetMinimap: panelControl = CreatePresetMinimap(ctlInfo); break;
                case WidgetType.Button:
                default: panelControl = CreateButton(ctlInfo); break;
            }

            if (CurrentMode == EngineMode.Designer) AttachDesignerEvents(panelControl, ctlInfo);

            _canvasPanel.Controls.Add(panelControl);
            panelControl.BringToFront(); 
        }

        private void AttachDesignerEvents(Control ctl, WidgetModel info)
        {
            ctl.MouseDown += (s, e) => HandleControlMouseDown(ctl, info, e);
            ctl.MouseMove += (s, e) => HandleControlMouseMove(ctl, e);
            ctl.MouseUp += (s, e) => HandleControlMouseUp(ctl, e);
            
            foreach(Control child in ctl.Controls)
            {
                child.MouseDown += (s, e) => { HandleControlMouseDown(ctl, info, e); };
                child.MouseMove += (s, e) => { HandleControlMouseMove(ctl, e); };
                child.MouseUp += (s, e) => { HandleControlMouseUp(ctl, e); };

                if (info.Type == WidgetType.SourceList) child.DoubleClick += (s, e) => OpenSourceWizard(info);
            }
            if (info.Type == WidgetType.SourceList) ctl.DoubleClick += (s, e) => OpenSourceWizard(info);
        }

        private void HandleControlMouseDown(Control parent, WidgetModel info, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PushUndoState();
                SelectControl(parent, info);
                _isDragging = true;
                _dragStartCursor = Cursor.Position;
                _dragStartLocation = parent.Location;
            }
        }
        
        private void HandleControlMouseMove(Control parent, MouseEventArgs e)
        {
            if (_isDragging && _selectedControl == parent)
            {
                Point cur = Cursor.Position;
                int dx = cur.X - _dragStartCursor.X;
                int dy = cur.Y - _dragStartCursor.Y;
                
                parent.Left = Snap(Math.Max(0, _dragStartLocation.X + dx));
                parent.Top = Snap(Math.Max(0, _dragStartLocation.Y + dy));
                
                UpdateHandlesPosition();
            }
        }

        private void HandleControlMouseUp(Control parent, MouseEventArgs e)
        {
            if (_isDragging) { _isDragging = false; UpdateModelFromControlBounds(); }
        }

        private void InitializeResizeHandles()
        {
            Cursor[] handleCursors = { Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE, Cursors.SizeWE, Cursors.SizeNESW, Cursors.SizeNS, Cursors.SizeNWSE };
            for (int i = 0; i < 8; i++)
            {
                var handle = new Panel
                {
                    Size = new Size(10, 10),BackColor = Color.Cyan, BorderStyle = BorderStyle.FixedSingle,
                    Cursor = handleCursors[i], Visible = false, Tag = i
                };
                
                handle.MouseDown += ResizeHandle_MouseDown;
                handle.MouseMove += ResizeHandle_MouseMove;
                handle.MouseUp += ResizeHandle_MouseUp;
                
                _resizeHandles.Add(handle);
                _canvasPanel.Controls.Add(handle);
            }
        }

        private void UpdateHandlesPosition()
        {
            if (_selectedControl == null) { foreach (var h in _resizeHandles) h.Visible = false; return; }

            Rectangle b = _selectedControl.Bounds;
            int hw = 5; 
            
            Point[] pts = {
                new Point(b.Left - hw, b.Top - hw), new Point(b.Left + b.Width/2 - hw, b.Top - hw), new Point(b.Right - hw, b.Top - hw),
                new Point(b.Left - hw, b.Top + b.Height/2 - hw), new Point(b.Right - hw, b.Top + b.Height/2 - hw),
                new Point(b.Left - hw, b.Bottom - hw), new Point(b.Left + b.Width/2 - hw, b.Bottom - hw), new Point(b.Right - hw, b.Bottom - hw)
            };

            for (int i = 0; i < 8; i++)
            {
                _resizeHandles[i].Location = pts[i];
                _resizeHandles[i].Visible = true;
                _resizeHandles[i].BringToFront(); 
            }
        }

        private void ResizeHandle_MouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is Panel handle && _selectedControl != null)
            {
                PushUndoState();
                handle.Capture = true; // [BUG FIX] 리사이징 도중 커서를 크게 벗어나면 컨트롤을 잃어버리는 현상 보완
                _isResizing = true;
                _resizeHandleIndex = handle.Tag != null ? (int)handle.Tag : -1;
                _dragStartCursor = Cursor.Position;
                _resizeStartBounds = _selectedControl.Bounds;
            }
        }

        private void ResizeHandle_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isResizing && _selectedControl != null)
            {
                Point cur = Cursor.Position;
                int dx = cur.X - _dragStartCursor.X;
                int dy = cur.Y - _dragStartCursor.Y;
                Rectangle n = _resizeStartBounds;

                switch (_resizeHandleIndex)
                {
                    case 0: n.X += dx; n.Width -= dx; n.Y += dy; n.Height -= dy; break;
                    case 1: n.Y += dy; n.Height -= dy; break;
                    case 2: n.Width += dx; n.Y += dy; n.Height -= dy; break;
                    case 3: n.X += dx; n.Width -= dx; break;
                    case 4: n.Width += dx; break;
                    case 5: n.X += dx; n.Width -= dx; n.Height += dy; break;
                    case 6: n.Height += dy; break;
                    case 7: n.Width += dx; n.Height += dy; break;
                }

                n.X = Snap(n.X); n.Y = Snap(n.Y); n.Width = Snap(n.Width); n.Height = Snap(n.Height);

                if (n.Width > 10 && n.Height > 10) { _selectedControl.Bounds = n; UpdateHandlesPosition(); }
            }
        }

        private void ResizeHandle_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                if (sender is Panel handle) handle.Capture = false;
                UpdateModelFromControlBounds();
                
                // 만약 현재 객체가 그리드 캔버스라면 자식들을 내부 규격에 맞게 리사이징함
                if (_selectedControl is Panel pnl && _propertyGrid?.SelectedObject is WidgetModel wm && wm.Type == WidgetType.GridCanvas)
                {
                    int cw = pnl.Width / Math.Max(1, wm.GridCols);
                    int ch = pnl.Height / Math.Max(1, wm.GridRows);
                    foreach(Control child in pnl.Controls)
                    {
                        if(child.Name.Contains("_R"))
                        {
                            var parts = child.Name.Split('_');
                            int _r = int.Parse(parts[parts.Length-2].Replace("R",""));
                            int _c = int.Parse(parts.Last().Replace("C",""));
                            child.Left = _c * cw; child.Top = _r * ch; child.Width = cw-1; child.Height = ch-1;
                        }
                    }
                }
            }
        }

        private void UpdateModelFromControlBounds()
        {
            if (_selectedControl != null && _propertyGrid?.SelectedObject is WidgetModel model)
            {
                model.X = _selectedControl.Left; model.Y = _selectedControl.Top;
                model.W = _selectedControl.Width; model.H = _selectedControl.Height;
                
                _propertyGrid.Refresh(); ProjectManager.Instance.SaveProject();
            }
        }

        private void ClearCanvas()
        {
            var widgetControls = _canvasPanel.Controls.OfType<Control>().Where(c => !_resizeHandles.Contains(c)).ToList();
            foreach (Control c in widgetControls) c.Dispose();
            _selectedControl = null;
            UpdateHandlesPosition();
        }

        private void OpenSourceWizard(WidgetModel model)
        {
            Form wizard = new Form { Width = 400, Height = 250, Text = "Source Registration Wizard", StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog };
            
            Label lId = new Label { Text = "Source ID (ex: PC_05):", Left=20, Top=20, Width=150 };
            TextBox txtId = new TextBox { Left=20, Top=40, Width=340 };
            
            Label lAlias = new Label { Text = "Source Alias (ex: Server C):", Left=20, Top=80, Width=150 };
            TextBox txtAlias = new TextBox { Left=20, Top=100, Width=340 };
            
            Button btnAdd = new Button { Text = "Add Item", Left=260, Top=150, Width=100, BackColor=Color.DarkGreen, ForeColor=Color.White, DialogResult = DialogResult.OK };
            
            wizard.Controls.Add(lId); wizard.Controls.Add(txtId);
            wizard.Controls.Add(lAlias); wizard.Controls.Add(txtAlias);
            wizard.Controls.Add(btnAdd); wizard.AcceptButton = btnAdd;

            if (wizard.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(txtId.Text))
            {
                PushUndoState();
                model.SourceItems.Add(new SourceItemNode { Id = txtId.Text, Alias = txtAlias.Text });
                ProjectManager.Instance.SaveProject();
                
                ClearCanvas(); 
                LoadLayoutFromJson(JsonSerializer.Serialize(ProjectManager.Instance.CurrentProject));
                DeselectAll(); 
            }
        }

        private Control CreateButton(WidgetModel info)
        {
            var btn = new Button
            {
                Name = info.Id, Text = info.Command, Left = Math.Max(0, info.X), Top = Math.Max(0, info.Y),
                Width = Math.Max(10, info.W), Height = Math.Max(10, info.H),
                BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Tag = info 
            };

            if (CurrentMode == EngineMode.Player)
            {
                btn.Click += async (s, e) =>
                {
                    if (s is Control && info != null)
                        await DeviceManager.Instance.SendCommandAsync(info.TargetDeviceId, info.Command);
                };
            }
            return btn;
        }

        private Control CreateGridCanvas(WidgetModel info)
        {
            var gridPanel = new Panel
            {
                Name = info.Id, Left = Math.Max(0, info.X), Top = Math.Max(0, info.Y), Width = Math.Max(10, info.W), Height = Math.Max(10, info.H),
                BackColor = Color.FromArgb(40, 40, 40), BorderStyle = BorderStyle.FixedSingle, Tag = info
            };

            int rows = Math.Max(1, info.GridRows);
            int cols = Math.Max(1, info.GridCols);
            int cellW = info.W / cols;
            int cellH = info.H / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = new Label
                    {
                        Name = $"{info.Id}_R{r}_C{c}", Text = $"Cell [{r},{c}]",
                        Left = c * cellW, Top = r * cellH, Width = cellW - 1, Height = cellH - 1,
                        BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.LightGray,
                        TextAlign = ContentAlignment.MiddleCenter, AllowDrop = true, BorderStyle = BorderStyle.FixedSingle
                    };

                    if (CurrentMode == EngineMode.Player)
                    {
                        int closureRow = r; int closureCol = c;
                        cell.DoubleClick += async (s, e) => await DeviceManager.Instance.IdentifyDeviceAsync(info.TargetDeviceId);
                        cell.DragEnter += (s, e) => e.Effect = e.Data!.GetDataPresent(typeof(SourceItemNode)) ? DragDropEffects.Copy : DragDropEffects.None;
                        cell.DragDrop += async (s, e) =>
                        {
                            if (e.Data!.GetData(typeof(SourceItemNode)) is SourceItemNode data)
                            {
                                cell.Text = data.Alias; cell.BackColor = Color.FromArgb(0, 100, 150);
                                await DeviceManager.Instance.SendCommandAsync(info.TargetDeviceId, $"MUX_{data.Id}_R{closureRow}_C{closureCol}");
                            }
                        };
                    }
                    gridPanel.Controls.Add(cell);
                }
            }
            return gridPanel;
        }

        private Control CreateLayoutChanger(WidgetModel info)
        {
            var btn = new Button
            {
                Name = info.Id, Text = info.Command, Left = Math.Max(0, info.X), Top = Math.Max(0, info.Y),
                Width = Math.Max(10, info.W), Height = Math.Max(10, info.H),
                BackColor = Color.FromArgb(100, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Tag = info 
            };

            if (CurrentMode == EngineMode.Player)
            {
                btn.Click += (s, e) =>
                {
                    var parts = info.Command.Split('_');
                    if (parts.Length == 3 && parts[0] == "RESIZE")
                    {
                        var config = ProjectManager.Instance.CurrentProject;
                        var targetGrid = config?.Widgets.FirstOrDefault(w => w.Type == WidgetType.GridCanvas && w.TargetDeviceId == info.TargetDeviceId);
                        if(targetGrid != null)
                        {
                            targetGrid.GridRows = int.Parse(parts[1]); targetGrid.GridCols = int.Parse(parts[2]);
                            ProjectManager.Instance.SaveProject(); LoadLayoutFromJson(JsonSerializer.Serialize(config));
                        }
                    }
                };
            }
            return btn;
        }

        private Control CreateSourceList(WidgetModel info)
        {
            var flowPanel = new FlowLayoutPanel
            {
                Name = info.Id, Left = Math.Max(0, info.X), Top = Math.Max(0, info.Y), Width = Math.Max(10, info.W), Height = Math.Max(10, info.H),
                BackColor = Color.FromArgb(30, 30, 30), AutoScroll = true, Tag = info
            };

            foreach (var src in info.SourceItems)
            {
                var srcLabel = new Label
                {
                    Text = src.Alias, Width = info.W - 25, Height = 40, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(5)
                };

                if (CurrentMode == EngineMode.Player)
                {
                    srcLabel.MouseDown += (s, e) => srcLabel.DoDragDrop(src, DragDropEffects.Copy);
                    srcLabel.DoubleClick += (s, e) =>
                    {
                        Form prompt = new Form { Width = 350, Height = 180, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Rename Alias", StartPosition = FormStartPosition.CenterScreen };
                        TextBox textBox = new TextBox { Left = 20, Top = 50, Width = 290, Text = src.Alias };
                        Button confirmation = new Button { Text = "Ok", Left = 210, Top = 90, Width = 100, DialogResult = DialogResult.OK };
                        prompt.Controls.Add(new Label { Left=20,Top=20,Text="New Alias:" }); prompt.Controls.Add(textBox); prompt.Controls.Add(confirmation); prompt.AcceptButton = confirmation;

                        if (prompt.ShowDialog() == DialogResult.OK) { src.Alias = textBox.Text; srcLabel.Text = src.Alias; ProjectManager.Instance.SaveProject(); }
                    };
                }
                flowPanel.Controls.Add(srcLabel);
            }
            return flowPanel;
        }

        private Control CreatePresetMinimap(WidgetModel info)
        {
            var pnl = new Panel { Name = info.Id, Left = Math.Max(0, info.X), Top = Math.Max(0, info.Y), Width = Math.Max(10, info.W), Height = Math.Max(10, info.H), BackColor = Color.Black, BorderStyle = BorderStyle.Fixed3D, Tag = info };
            
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics; var grid = ProjectManager.Instance.CurrentProject?.Widgets.FirstOrDefault(w => w.Type == WidgetType.GridCanvas);
                if(grid != null)
                {
                    float cw = pnl.Width / (float)Math.Max(1, grid.GridCols); float ch = pnl.Height / (float)Math.Max(1, grid.GridRows);
                    using var pen = new Pen(Color.Lime, 2f);
                    for(int i = 0; i < grid.GridRows; i++) for(int j = 0; j < grid.GridCols; j++) g.DrawRectangle(pen, j * cw, i * ch, cw, ch);
                    g.DrawString($"Layout: {grid.GridRows}x{grid.GridCols}", new Font("Arial", 10), Brushes.White, 5, 5);
                }
            };
            
            pnl.Resize += (s, e) => pnl.Invalidate();
            if (CurrentMode == EngineMode.Player) pnl.Click += async (s, e) => await PresetManager.Instance.ExecutePresetAsync(new PresetState { Name = "Full Restore" });
            
            return pnl;
        }
    }
}
