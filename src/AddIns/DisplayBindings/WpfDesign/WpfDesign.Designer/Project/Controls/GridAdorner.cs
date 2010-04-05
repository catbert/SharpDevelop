// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.WpfDesign.Adorners;

namespace ICSharpCode.WpfDesign.Designer.Controls
{
	/// <summary>
	/// Adorner that displays the blue bar next to grids that can be used to create new rows/column.
	/// </summary>
	public class GridRailAdorner : FrameworkElement
	{
		static GridRailAdorner()
		{
			bgBrush = new SolidColorBrush(Color.FromArgb(0x30, 0x20, 0x20, 0xff));
			bgBrush.Freeze();
			
			selBrush = new SolidColorBrush(Color.FromArgb(0xC0, 0xff, 0xb7, 0x4f));
			selBrush.Freeze();
		}
		
		readonly DesignItem gridItem;
		readonly Grid grid;
		readonly AdornerPanel adornerPanel;
		readonly GridSplitterAdorner previewAdorner;
		readonly Orientation orientation;
		readonly GridUnitSelector unitSelector;
		
		static readonly SolidColorBrush bgBrush;
		static readonly SolidColorBrush selBrush;
		
		public const double RailSize = 10;
		public const double RailDistance = 6;
		public const double SplitterWidth = 10;
		
		public GridRailAdorner(DesignItem gridItem, AdornerPanel adornerPanel, Orientation orientation)
		{
			Debug.Assert(gridItem != null);
			Debug.Assert(adornerPanel != null);
			
			this.gridItem = gridItem;
			this.grid = (Grid)gridItem.Component;
			this.adornerPanel = adornerPanel;
			this.orientation = orientation;
			
			this.unitSelector = new GridUnitSelector(this);
			
			adornerPanel.Children.Add(unitSelector);
			
			if (orientation == Orientation.Horizontal) {
				this.Height = RailSize;
				previewAdorner = new GridColumnSplitterAdorner(this, gridItem, null, null);
			} else { // vertical
				this.Width = RailSize;
				previewAdorner = new GridRowSplitterAdorner(this, gridItem, null, null);
			}
			unitSelector.Orientation = orientation;
			previewAdorner.IsPreview = true;
			previewAdorner.IsHitTestVisible = false;
		}
		
		public void SelectionChanged()
		{
			if (orientation == Orientation.Vertical) {
				if (gridItem.Properties["RowDefinitions"].CollectionElements.Any(e => gridItem.Services.Selection.IsComponentSelected(e)))
					unitSelector.Visibility = Visibility.Visible;
				else
					unitSelector.Visibility = Visibility.Hidden;
			} else {
				if (gridItem.Properties["ColumnDefinitions"].CollectionElements.Any(e => gridItem.Services.Selection.IsComponentSelected(e)))
					unitSelector.Visibility = Visibility.Visible;
				else
					unitSelector.Visibility = Visibility.Hidden;
			}
			InvalidateVisual();
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			
			if (orientation == Orientation.Horizontal) {
				Rect bgRect = new Rect(0, 0, grid.ActualWidth, RailSize);
				drawingContext.DrawRectangle(bgBrush, null, bgRect);
				
				DesignItemProperty colCollection = gridItem.Properties["ColumnDefinitions"];
				foreach (var colItem in colCollection.CollectionElements) {
					ColumnDefinition column = colItem.Component as ColumnDefinition;
					if (column.ActualWidth < 0) continue;
					Rect selRect = new Rect(column.Offset, 0, column.ActualWidth, RailSize);
					GridLength len = (GridLength)column.GetValue(ColumnDefinition.WidthProperty);
					
					FormattedText text = new FormattedText(GridLengthToText(len), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Sergio UI"), 10, Brushes.Black);
					text.TextAlignment = TextAlignment.Center;
					if (gridItem.Services.Selection.IsComponentSelected(colItem)) {
						drawingContext.DrawRectangle(selBrush, null, selRect);
					}
					drawingContext.DrawText(text, new Point(column.Offset + column.ActualWidth / 2, 0));
				}
			} else {
				Rect bgRect = new Rect(0, 0, RailSize, grid.ActualHeight);
				drawingContext.DrawRectangle(bgBrush, null, bgRect);
				
				DesignItemProperty rowCollection = gridItem.Properties["RowDefinitions"];
				foreach (var rowItem in rowCollection.CollectionElements) {
					RowDefinition row = rowItem.Component as RowDefinition;
					if (row.ActualHeight < 0) continue;
					Rect selRect = new Rect(0, row.Offset, RailSize, row.ActualHeight);
					
					GridLength len = (GridLength)row.GetValue(RowDefinition.HeightProperty);
					
					FormattedText text = new FormattedText(GridLengthToText(len), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Sergio UI"), 10, Brushes.Black);
					text.TextAlignment = TextAlignment.Center;
					if (gridItem.Services.Selection.IsComponentSelected(rowItem)) {
						drawingContext.DrawRectangle(selBrush, null, selRect);
					}
					drawingContext.PushTransform(new RotateTransform(-270));
					drawingContext.PushTransform(new TranslateTransform(0, -10));
					drawingContext.DrawText(text, new Point(row.Offset + row.ActualHeight / 2, 0));
					drawingContext.Pop();
					drawingContext.Pop();
				}
			}
		}
		
		#region Handle mouse events to add a new row/column
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			
			if (orientation == Orientation.Vertical) {
				double insertionPosition = e.GetPosition(this).Y;
				RowDefinition current = grid.RowDefinitions
					.FirstOrDefault(r => insertionPosition >= r.Offset &&
					                insertionPosition <= (r.Offset + r.ActualHeight));
				
				if (current == null || !gridItem.Services.Selection.IsComponentSelected(gridItem.Services.Component.GetDesignItem(current)))
					return;
			} else {
				double insertionPosition = e.GetPosition(this).X;
				ColumnDefinition current = grid.ColumnDefinitions
					.FirstOrDefault(r => insertionPosition >= r.Offset &&
					                insertionPosition <= (r.Offset + r.ActualWidth));
				
				if (current == null || !gridItem.Services.Selection.IsComponentSelected(gridItem.Services.Component.GetDesignItem(current)))
					return;
			}
			
			adornerPanel.Children.Add(previewAdorner);
		}
		
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			RelativePlacement rp = new RelativePlacement();
			RelativePlacement rpUnitSelector = new RelativePlacement();
			if (orientation == Orientation.Vertical) {
				double insertionPosition = e.GetPosition(this).Y;
				RowDefinition current = grid.RowDefinitions
					.FirstOrDefault(r => insertionPosition >= r.Offset &&
					                insertionPosition <= (r.Offset + r.ActualHeight));
				
				DesignItem component;
				
				if (current == null || !gridItem.Services.Selection.IsComponentSelected(component = gridItem.Services.Component.GetDesignItem(current)))
					return;
				
				rp.XOffset = -(RailSize + RailDistance);
				rp.WidthOffset = RailSize + RailDistance;
				rp.WidthRelativeToContentWidth = 1;
				rp.HeightOffset = SplitterWidth;
				rp.YOffset = e.GetPosition(this).Y - SplitterWidth / 2;
				
				rpUnitSelector.XOffset = -(RailSize + RailDistance) * 2.75;
				rpUnitSelector.WidthOffset = RailSize + RailDistance;
				rpUnitSelector.WidthRelativeToContentWidth = 1;
				rpUnitSelector.HeightOffset = 55;
				rpUnitSelector.YOffset = current.Offset + current.ActualHeight / 2 - 25;
				unitSelector.SelectedItem = component;
				unitSelector.Unit = ((GridLength)component.Properties[RowDefinition.HeightProperty].ValueOnInstance).GridUnitType;
			} else {
				double insertionPosition = e.GetPosition(this).X;
				ColumnDefinition current = grid.ColumnDefinitions
					.FirstOrDefault(r => insertionPosition >= r.Offset &&
					                insertionPosition <= (r.Offset + r.ActualWidth));
				
				DesignItem component;
				
				if (current == null || !gridItem.Services.Selection.IsComponentSelected(component = gridItem.Services.Component.GetDesignItem(current)))
					return;
				
				rp.YOffset = -(RailSize + RailDistance);
				rp.HeightOffset = RailSize + RailDistance;
				rp.HeightRelativeToContentHeight = 1;
				rp.WidthOffset = SplitterWidth;
				rp.XOffset = e.GetPosition(this).X - SplitterWidth / 2;
				
				rpUnitSelector.YOffset = -(RailSize + RailDistance) * 2.75;
				rpUnitSelector.HeightOffset = RailSize + RailDistance;
				rpUnitSelector.HeightRelativeToContentHeight = 1;
				rpUnitSelector.WidthOffset = 75;
				rpUnitSelector.XOffset = current.Offset + current.ActualWidth / 2 - 35;
				unitSelector.SelectedItem = component;
				unitSelector.Unit = ((GridLength)component.Properties[ColumnDefinition.WidthProperty].ValueOnInstance).GridUnitType;
			}
			
			AdornerPanel.SetPlacement(previewAdorner, rp);
			AdornerPanel.SetPlacement(unitSelector, rpUnitSelector);
		}
		
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			adornerPanel.Children.Remove(previewAdorner);
		}
		
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			e.Handled = true;
			Focus();
			adornerPanel.Children.Remove(previewAdorner);
			if (orientation == Orientation.Vertical) {
				double insertionPosition = e.GetPosition(this).Y;
				DesignItemProperty rowCollection = gridItem.Properties["RowDefinitions"];
				
				DesignItem currentRow = null;
				
				using (ChangeGroup changeGroup = gridItem.OpenGroup("Split grid row")) {
					if (rowCollection.CollectionElements.Count == 0) {
						DesignItem firstRow = gridItem.Services.Component.RegisterComponentForDesigner(new RowDefinition());
						rowCollection.CollectionElements.Add(firstRow);
						grid.UpdateLayout(); // let WPF assign firstRow.ActualHeight
						
						currentRow = firstRow;
					} else {
						RowDefinition current = grid.RowDefinitions
							.FirstOrDefault(r => insertionPosition >= r.Offset &&
							                insertionPosition <= (r.Offset + r.ActualHeight));
						if (current != null)
							currentRow = gridItem.Services.Component.GetDesignItem(current);
					}
					
					if (currentRow == null)
						currentRow = gridItem.Services.Component.GetDesignItem(grid.RowDefinitions.Last());
					
					unitSelector.SelectedItem = currentRow;
					
					if (!gridItem.Services.Selection.IsComponentSelected(currentRow)) {
						gridItem.Services.Selection.SetSelectedComponents(new DesignItem[] { currentRow }, SelectionTypes.Auto);
						changeGroup.Commit();
						if (!adornerPanel.Children.Contains(previewAdorner))
							adornerPanel.Children.Add(previewAdorner);
						
						OnMouseMove(e);
						InvalidateVisual();
						return;
					}

					for (int i = 0; i < grid.RowDefinitions.Count; i++) {
						RowDefinition row = grid.RowDefinitions[i];
						if (row.Offset > insertionPosition) continue;
						if (row.Offset + row.ActualHeight < insertionPosition) continue;
						
						// split row
						GridLength oldLength = (GridLength)row.GetValue(RowDefinition.HeightProperty);
						GridLength newLength1, newLength2;
						SplitLength(oldLength, insertionPosition - row.Offset, row.ActualHeight, out newLength1, out newLength2);
						DesignItem newRowDefinition = gridItem.Services.Component.RegisterComponentForDesigner(new RowDefinition());
						rowCollection.CollectionElements.Insert(i + 1, newRowDefinition);
						rowCollection.CollectionElements[i].Properties[RowDefinition.HeightProperty].SetValue(newLength1);
						newRowDefinition.Properties[RowDefinition.HeightProperty].SetValue(newLength2);
						
						FixIndicesAfterSplit(i, Grid.RowProperty, Grid.RowSpanProperty);
						changeGroup.Commit();
						gridItem.Services.Selection.SetSelectedComponents(new DesignItem[] { newRowDefinition }, SelectionTypes.Auto);
						break;
					}
				}
			} else {
				double insertionPosition = e.GetPosition(this).X;
				DesignItemProperty columnCollection = gridItem.Properties["ColumnDefinitions"];
				
				DesignItem currentColumn = null;
				
				using (ChangeGroup changeGroup = gridItem.OpenGroup("Split grid column")) {
					if (columnCollection.CollectionElements.Count == 0) {
						DesignItem firstColumn = gridItem.Services.Component.RegisterComponentForDesigner(new ColumnDefinition());
						columnCollection.CollectionElements.Add(firstColumn);
						grid.UpdateLayout(); // let WPF assign firstColumn.ActualWidth
						
						currentColumn = firstColumn;
					} else {
						ColumnDefinition current = grid.ColumnDefinitions
							.FirstOrDefault(r => insertionPosition >= r.Offset &&
							                insertionPosition <= (r.Offset + r.ActualWidth));
						if (current != null)
							currentColumn = gridItem.Services.Component.GetDesignItem(current);
					}
					
					if (currentColumn == null)
						currentColumn = gridItem.Services.Component.GetDesignItem(grid.ColumnDefinitions.Last());
					
					unitSelector.SelectedItem = currentColumn;
					
					if (!gridItem.Services.Selection.IsComponentSelected(currentColumn)) {
						gridItem.Services.Selection.SetSelectedComponents(new DesignItem[] { currentColumn }, SelectionTypes.Auto);
						changeGroup.Commit();
						if (!adornerPanel.Children.Contains(previewAdorner))
							adornerPanel.Children.Add(previewAdorner);
						OnMouseMove(e);
						InvalidateVisual();
						return;
					}

					for (int i = 0; i < grid.ColumnDefinitions.Count; i++) {
						ColumnDefinition column = grid.ColumnDefinitions[i];
						if (column.Offset > insertionPosition) continue;
						if (column.Offset + column.ActualWidth < insertionPosition) continue;
						
						// split column
						GridLength oldLength = (GridLength)column.GetValue(ColumnDefinition.WidthProperty);
						GridLength newLength1, newLength2;
						SplitLength(oldLength, insertionPosition - column.Offset, column.ActualWidth, out newLength1, out newLength2);
						DesignItem newColumnDefinition = gridItem.Services.Component.RegisterComponentForDesigner(new ColumnDefinition());
						columnCollection.CollectionElements.Insert(i + 1, newColumnDefinition);
						columnCollection.CollectionElements[i].Properties[ColumnDefinition.WidthProperty].SetValue(newLength1);
						newColumnDefinition.Properties[ColumnDefinition.WidthProperty].SetValue(newLength2);
						FixIndicesAfterSplit(i, Grid.ColumnProperty, Grid.ColumnSpanProperty);
						changeGroup.Commit();
						gridItem.Services.Selection.SetSelectedComponents(new DesignItem[] { newColumnDefinition }, SelectionTypes.Auto);
						break;
					}
				}
			}
		}
		
		void FixIndicesAfterSplit(int splitIndex, DependencyProperty idxProperty, DependencyProperty spanProperty)
		{
			// increment ColSpan of all controls in the split column, increment Column of all controls in later columns:
			foreach (DesignItem child in gridItem.Properties["Children"].CollectionElements) {
				int start = (int)child.Properties.GetAttachedProperty(idxProperty).ValueOnInstance;
				int span = (int)child.Properties.GetAttachedProperty(spanProperty).ValueOnInstance;
				if (start <= splitIndex && splitIndex < start + span) {
					child.Properties.GetAttachedProperty(spanProperty).SetValue(span + 1);
				} else if (start > splitIndex) {
					child.Properties.GetAttachedProperty(idxProperty).SetValue(start + 1);
				}
			}
		}
		
		static void SplitLength(GridLength oldLength, double insertionPosition, double oldActualValue,
		                        out GridLength newLength1, out GridLength newLength2)
		{
			if (oldLength.IsAuto) {
				oldLength = new GridLength(oldActualValue);
			}
			double percentage = insertionPosition / oldActualValue;
			newLength1 = new GridLength(oldLength.Value * percentage, oldLength.GridUnitType);
			newLength2 = new GridLength(oldLength.Value - newLength1.Value, oldLength.GridUnitType);
		}
		#endregion
		
		string GridLengthToText(GridLength len)
		{
			switch (len.GridUnitType) {
				case GridUnitType.Auto:
					return "Auto";
				case GridUnitType.Star:
					return len.Value == 1 ? "*" : Math.Round(len.Value, 2) + "*";
				case GridUnitType.Pixel:
					return Math.Round(len.Value, 2) + "px";
			}
			return string.Empty;
		}
		
		public void SetGridLengthUnit(GridUnitType unit)
		{
			DesignItem item = unitSelector.SelectedItem;
			GridLength value;
			
			grid.UpdateLayout();
			
			Debug.Assert(item != null);
			
			if (orientation == Orientation.Vertical) {
				value = (GridLength)item.Properties[RowDefinition.HeightProperty].ValueOnInstance;

				if (unit == GridUnitType.Auto)
					value = GridLength.Auto;
				else
					value = new GridLength(value.Value, unit);

				item.Properties[RowDefinition.HeightProperty].SetValue(value);
			} else {
				value = (GridLength)item.Properties[ColumnDefinition.WidthProperty].ValueOnInstance;

				if (unit == GridUnitType.Auto)
					value = GridLength.Auto;
				else
					value = new GridLength(value.Value, unit);

				item.Properties[ColumnDefinition.WidthProperty].SetValue(value);
			}
			grid.UpdateLayout();
			InvalidateVisual();
		}
	}
	
	public abstract class GridSplitterAdorner : Control
	{
		public static readonly DependencyProperty IsPreviewProperty
			= DependencyProperty.Register("IsPreview", typeof(bool), typeof(GridSplitterAdorner), new PropertyMetadata(SharedInstances.BoxedFalse));
		
		protected readonly Grid grid;
		protected readonly DesignItem gridItem;
		protected readonly DesignItem firstRow, secondRow; // can also be columns
		protected readonly GridRailAdorner rail;
		
		internal GridSplitterAdorner(GridRailAdorner rail, DesignItem gridItem, DesignItem firstRow, DesignItem secondRow)
		{
			Debug.Assert(gridItem != null);
			this.grid = (Grid)gridItem.Component;
			this.gridItem = gridItem;
			this.firstRow = firstRow;
			this.secondRow = secondRow;
			this.rail = rail;
		}
		
		public bool IsPreview {
			get { return (bool)GetValue(IsPreviewProperty); }
			set { SetValue(IsPreviewProperty, SharedInstances.Box(value)); }
		}
		
		ChangeGroup activeChangeGroup;
		double mouseStartPos;
		bool mouseIsDown;
		
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			e.Handled = true;
			if (CaptureMouse()) {
				Focus();
				gridItem.Services.Selection.SetSelectedComponents(new DesignItem[] { secondRow }, SelectionTypes.Auto);
				mouseStartPos = GetCoordinate(e.GetPosition(grid));
				mouseIsDown = true;
			}
		}
		
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (mouseIsDown) {
				double mousePos = GetCoordinate(e.GetPosition(grid));
				if (activeChangeGroup == null) {
					if (Math.Abs(mousePos - mouseStartPos)
					    >= GetCoordinate(new Point(SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance))) {
						activeChangeGroup = gridItem.OpenGroup("Change grid row/column size");
						RememberOriginalSize();
					}
				}
				if (activeChangeGroup != null) {
					ChangeSize(mousePos - mouseStartPos);
				}
			}
		}
		
		protected GridLength original1, original2;
		protected double originalPixelSize1, originalPixelSize2;
		
		protected abstract double GetCoordinate(Point point);
		protected abstract void RememberOriginalSize();
		protected abstract DependencyProperty RowColumnSizeProperty { get; }
		
		void ChangeSize(double delta)
		{
			// delta = difference in pixels
			
			if (delta < -originalPixelSize1) delta = -originalPixelSize1;
			if (delta > originalPixelSize2) delta = originalPixelSize2;
			
			// replace Auto lengths with absolute lengths if necessary
			if (original1.IsAuto) original1 = new GridLength(originalPixelSize1);
			if (original2.IsAuto) original2 = new GridLength(originalPixelSize2);
			
			GridLength new1;
			if (original1.IsStar && originalPixelSize1 > 0)
				new1 = new GridLength(original1.Value * (originalPixelSize1 + delta) / originalPixelSize1, GridUnitType.Star);
			else
				new1 = new GridLength(originalPixelSize1 + delta);
			GridLength new2;
			if (original2.IsStar && originalPixelSize2 > 0)
				new2 = new GridLength(original2.Value * (originalPixelSize2 - delta) / originalPixelSize2, GridUnitType.Star);
			else
				new2 = new GridLength(originalPixelSize2 - delta);
			firstRow.Properties[RowColumnSizeProperty].SetValue(new1);
			secondRow.Properties[RowColumnSizeProperty].SetValue(new2);
			UIElement e = ((UIElement)VisualTreeHelper.GetParent(this));
			e.InvalidateArrange();
			rail.InvalidateVisual();
		}
		
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (activeChangeGroup != null) {
				activeChangeGroup.Commit();
				activeChangeGroup = null;
			}
			Stop();
		}
		
		protected override void OnLostMouseCapture(MouseEventArgs e)
		{
			Stop();
		}
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape) {
				e.Handled = true;
				Stop();
			}
		}
		
		protected void Stop()
		{
			ReleaseMouseCapture();
			mouseIsDown = false;
			if (activeChangeGroup != null) {
				activeChangeGroup.Abort();
				activeChangeGroup = null;
			}
		}
	}
	
	public class GridRowSplitterAdorner : GridSplitterAdorner
	{
		static GridRowSplitterAdorner()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GridRowSplitterAdorner), new FrameworkPropertyMetadata(typeof(GridRowSplitterAdorner)));
			CursorProperty.OverrideMetadata(typeof(GridRowSplitterAdorner), new FrameworkPropertyMetadata(Cursors.SizeNS));
		}
		
		internal GridRowSplitterAdorner(GridRailAdorner rail, DesignItem gridItem, DesignItem firstRow, DesignItem secondRow)
			: base(rail, gridItem, firstRow, secondRow)
		{
		}
		
		protected override double GetCoordinate(Point point)
		{
			return point.Y;
		}
		
		protected override void RememberOriginalSize()
		{
			RowDefinition r1 = (RowDefinition)firstRow.Component;
			RowDefinition r2 = (RowDefinition)secondRow.Component;
			original1 = (GridLength)r1.GetValue(RowDefinition.HeightProperty);
			original2 = (GridLength)r2.GetValue(RowDefinition.HeightProperty);
			originalPixelSize1 = r1.ActualHeight;
			originalPixelSize2 = r2.ActualHeight;
		}
		
		protected override DependencyProperty RowColumnSizeProperty {
			get { return RowDefinition.HeightProperty; }
		}
	}
	
	public sealed class GridColumnSplitterAdorner : GridSplitterAdorner
	{
		static GridColumnSplitterAdorner()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GridColumnSplitterAdorner), new FrameworkPropertyMetadata(typeof(GridColumnSplitterAdorner)));
			CursorProperty.OverrideMetadata(typeof(GridColumnSplitterAdorner), new FrameworkPropertyMetadata(Cursors.SizeWE));
		}
		
		internal GridColumnSplitterAdorner(GridRailAdorner rail, DesignItem gridItem, DesignItem firstRow, DesignItem secondRow)
			: base(rail, gridItem, firstRow, secondRow)
		{
		}
		
		protected override double GetCoordinate(Point point)
		{
			return point.X;
		}
		
		protected override void RememberOriginalSize()
		{
			ColumnDefinition r1 = (ColumnDefinition)firstRow.Component;
			ColumnDefinition r2 = (ColumnDefinition)secondRow.Component;
			original1 = (GridLength)r1.GetValue(ColumnDefinition.WidthProperty);
			original2 = (GridLength)r2.GetValue(ColumnDefinition.WidthProperty);
			originalPixelSize1 = r1.ActualWidth;
			originalPixelSize2 = r2.ActualWidth;
		}
		
		protected override DependencyProperty RowColumnSizeProperty {
			get { return ColumnDefinition.WidthProperty; }
		}
	}
}