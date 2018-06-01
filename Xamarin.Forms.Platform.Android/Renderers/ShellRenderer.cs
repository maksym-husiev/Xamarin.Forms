﻿using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.ComponentModel;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using LP = Android.Views.ViewGroup.LayoutParams;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Xamarin.Forms.Platform.Android
{
	public class CustomFrameLayout : FrameLayout
	{
		public CustomFrameLayout(Context context) : base(context)
		{
		}

		public CustomFrameLayout(Context context, IAttributeSet attrs) : base(context, attrs)
		{
		}

		public CustomFrameLayout(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
		}

		public CustomFrameLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
		{
		}

		protected CustomFrameLayout(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public override WindowInsets OnApplyWindowInsets(WindowInsets insets)
		{
			// We need to make sure we retain left padding.
			// Failure to do so will result in the padding being wrong if you set FlyoutBehavior to Locked
			// and then rotate the device.

			var leftPadding = PaddingLeft;

			var result = base.OnApplyWindowInsets(insets);

			SetPadding(leftPadding, PaddingTop, PaddingRight, PaddingBottom);

			return result;
		}
	}


	public class ShellRenderer : IVisualElementRenderer, IShellContext, IAppearanceObserver
	{
		#region IVisualElementRenderer

		event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
		{
			add { _elementChanged += value; }
			remove { _elementChanged -= value; }
		}

		event EventHandler<PropertyChangedEventArgs> IVisualElementRenderer.ElementPropertyChanged
		{
			add { _elementPropertyChanged += value; }
			remove { _elementPropertyChanged -= value; }
		}

		VisualElement IVisualElementRenderer.Element => Element;

		VisualElementTracker IVisualElementRenderer.Tracker => null;

		AView IVisualElementRenderer.View => _flyoutRenderer.AndroidView;
		ViewGroup IVisualElementRenderer.ViewGroup => _flyoutRenderer.AndroidView as ViewGroup;

		SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			return new SizeRequest(new Size(100, 100));
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			if (Element != null)
				throw new NotSupportedException("Reuse of the Shell Renderer is not supported");
			Element = (Shell)element;
			Element.SizeChanged += OnElementSizeChanged;
			OnElementSet(Element);

			Element.PropertyChanged += OnElementPropertyChanged;
			_elementChanged?.Invoke(this, new VisualElementChangedEventArgs(null, Element));
		}

		void IVisualElementRenderer.SetLabelFor(int? id)
		{
		}

		void IVisualElementRenderer.UpdateLayout()
		{
			var width = (int)AndroidContext.ToPixels(Element.Width);
			var height = (int)AndroidContext.ToPixels(Element.Height);
			_flyoutRenderer.AndroidView.Layout(0, 0, width, height);
		}

		#endregion IVisualElementRenderer

		#region IShellContext

		Context IShellContext.AndroidContext => AndroidContext;

		// This is very bad, FIXME.
		// This assumes all flyouts will implement via DrawerLayout which is PROBABLY true but
		// I dont want to back us into a corner this time.
		DrawerLayout IShellContext.CurrentDrawerLayout => (DrawerLayout)_flyoutRenderer.AndroidView;

		Shell IShellContext.Shell => Element;

		IShellObservableFragment IShellContext.CreateFragmentForPage(Page page)
		{
			return CreateFragmentForPage(page);
		}

		IShellFlyoutContentRenderer IShellContext.CreateShellFlyoutContentRenderer()
		{
			var content = CreateShellFlyoutContentRenderer();

			content.ElementSelected += OnFlyoutItemSelected;

			return content;
		}

		IShellItemRenderer IShellContext.CreateShellItemRenderer(ShellItem shellItem)
		{
			return CreateShellItemRenderer(shellItem);
		}

		IShellToolbarTracker IShellContext.CreateTrackerForToolbar(Toolbar toolbar)
		{
			return CreateTrackerForToolbar(toolbar);
		}

		IShellToolbarAppearanceTracker IShellContext.CreateToolbarAppearanceTracker()
		{
			return CreateToolbarAppearanceTracker();
		}

		IShellTabLayoutAppearanceTracker IShellContext.CreateTabLayoutAppearanceTracker(ShellSection shellContent)
		{
			return CreateTabLayoutAppearanceTracker(shellContent);
		}

		IShellBottomNavViewAppearanceTracker IShellContext.CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
		{
			return CreateBottomNavViewAppearanceTracker(shellItem);
		}

		#endregion IShellContext

		#region IAppearanceObserver

		void IAppearanceObserver.OnAppearanceChanged(ShellAppearance appearance)
		{
			UpdateStatusBarColor(appearance);
		}

		#endregion IAppearanceObserver

		public static readonly Color DefaultBackgroundColor = Color.FromRgb(33, 150, 243);
		public static readonly Color DefaultForegroundColor = Color.White;
		public static readonly Color DefaultTitleColor = Color.White;
		public static readonly Color DefaultUnselectedColor = Color.FromRgba(255, 255, 255, 180);

		private bool _disposed;
		private IShellFlyoutRenderer _flyoutRenderer;
		private FrameLayout _frameLayout;

		public ShellRenderer(Context context)
		{
			AndroidContext = context;
		}

		private event EventHandler<VisualElementChangedEventArgs> _elementChanged;

		private event EventHandler<PropertyChangedEventArgs> _elementPropertyChanged;

		protected Context AndroidContext { get; }
		protected Shell Element { get; private set; }
		private FragmentManager FragmentManager => ((FormsAppCompatActivity)AndroidContext).SupportFragmentManager;

		protected virtual IShellObservableFragment CreateFragmentForPage(Page page)
		{
			return new ShellContentFragment(this, page);
		}

		protected virtual IShellFlyoutContentRenderer CreateShellFlyoutContentRenderer()
		{
			return new ShellFlyoutTemplatedContentRenderer(this);
			//return new ShellFlyoutContentRenderer(this, AndroidContext);
		}

		protected virtual IShellFlyoutRenderer CreateShellFlyoutRenderer()
		{
			return new ShellFlyoutRenderer(this, AndroidContext);
		}

		protected virtual IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
		{
			//var tabLocation = shellItem.OnThisPlatform().GetTabBarLocation();
			//if (tabLocation == ShellTabBarLocation.Bottom)
				return new ShellItemRenderer(this);
			//return new ShellTopTabItemRenderer(this);
		}

		protected virtual IShellToolbarTracker CreateTrackerForToolbar(Toolbar toolbar)
		{
			return new ShellToolbarTracker(this, toolbar, ((IShellContext)this).CurrentDrawerLayout);
		}

		protected virtual IShellToolbarAppearanceTracker CreateToolbarAppearanceTracker()
		{
			return new ShellToolbarAppearanceTracker(this);
		}

		protected virtual IShellTabLayoutAppearanceTracker CreateTabLayoutAppearanceTracker(ShellSection shellSection)
		{
			return new ShellTabLayoutAppearanceTracker(this);
		}

		protected virtual IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
		{
			return new ShellBottomNavViewAppearanceTracker(this, shellItem);
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Shell.CurrentItemProperty.PropertyName)
			{
				SwitchFragment(FragmentManager, _frameLayout, Element.CurrentItem);
			}

			_elementPropertyChanged?.Invoke(sender, e);
		}

		protected virtual void OnElementSet(Shell shell)
		{
			_flyoutRenderer = CreateShellFlyoutRenderer();
			_frameLayout = new CustomFrameLayout(AndroidContext)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
				Id = Platform.GenerateViewId(),
			};
			_frameLayout.SetFitsSystemWindows(true);

			_flyoutRenderer.AttachFlyout(this, _frameLayout);

			((IShellController)shell).AddAppearanceObserver(this, shell);

			SwitchFragment(FragmentManager, _frameLayout, shell.CurrentItem, false);
		}

		protected virtual void SwitchFragment(FragmentManager manager, AView targetView, ShellItem newItem, bool animate = true)
		{
			var shellItemRenderer = CreateShellItemRenderer(newItem);
			shellItemRenderer.ShellItem = newItem;
			var fragment = shellItemRenderer.Fragment;

			FragmentTransaction transaction = manager.BeginTransaction();

			if (animate)
				transaction.SetTransition((int)global::Android.App.FragmentTransit.FragmentFade);

			transaction.Replace(_frameLayout.Id, fragment);
			transaction.CommitAllowingStateLoss();
		}

		private async void GoTo(ShellItem item, ShellSection shellSection, ShellContent shellContent)
		{
			if (shellSection == null)
				shellSection = item?.CurrentItem;
			if (shellContent == null)
				shellContent = shellSection?.CurrentItem;
			var state = ((IShellController)Element).GetNavigationState(item, shellSection, shellContent, false);
			await Element.GoToAsync(state).ConfigureAwait(false);
		}

		private void OnElementSizeChanged(object sender, EventArgs e)
		{
			int width = (int)AndroidContext.ToPixels(Element.Width);
			int height = (int)AndroidContext.ToPixels(Element.Height);
			_flyoutRenderer.AndroidView.Measure(MeasureSpecFactory.MakeMeasureSpec(width, MeasureSpecMode.Exactly), 
				MeasureSpecFactory.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
			_flyoutRenderer.AndroidView.Layout(0, 0, width, height);
		}

		private void OnFlyoutItemSelected(object sender, ElementSelectedEventArgs e)
		{
			var element = e.Element;
			ShellItem shellItem = null;
			ShellSection shellSection = null;
			ShellContent shellContent = null;

			if (element is ShellItem.MenuShellItem menuShellItem)
			{
				menuShellItem.MenuItem.Activate();
			}
			else if (element is ShellItem i)
			{
				shellItem = i;
			}
			else if (element is ShellSection s)
			{
				shellItem = s.Parent as ShellItem;
				shellSection = s;
			}
			else if (element is ShellContent c)
			{
				shellItem = c.Parent.Parent as ShellItem;
				shellSection = c.Parent as ShellSection;
				shellContent = c;
			}
			else if (element is MenuItem m)
			{
				m.Activate();
			}

			_flyoutRenderer.CloseFlyout();
			if (shellItem != null && shellItem.IsEnabled)
				GoTo(shellItem, shellSection, shellContent);
		}

		private void UpdateStatusBarColor(ShellAppearance appearance)
		{
			var activity = ((FormsAppCompatActivity)AndroidContext);
			var window = activity.Window;
			var decorView = window.DecorView;
			var resources = activity.Resources;

			int statusBarHeight = 0;
			int resourceId = resources.GetIdentifier("status_bar_height", "dimen", "android");
			if (resourceId > 0)
			{
				statusBarHeight = resources.GetDimensionPixelSize(resourceId);
			}

			int navigationBarHeight = 0;
			resourceId = resources.GetIdentifier("navigation_bar_height", "dimen", "android");
			if (resourceId > 0)
			{
				navigationBarHeight = resources.GetDimensionPixelSize(resourceId);
			}

			// we are using the split drawable here to avoid GPU overdraw.
			// All it really is is a drawable that only draws under the statusbar/bottom bar to make sure
			// we dont draw over areas we dont need to. This has very limited benefits considering its
			// only saving us a flat color fill BUT it helps people not freak out about overdraw.
			if (appearance != null)
			{
				var color = appearance.BackgroundColor.ToAndroid(Color.FromHex("#03A9F4"));
				decorView.SetBackground(new SplitDrawable(color, statusBarHeight, navigationBarHeight));
			}
			else
			{
				var color = Color.FromHex("#03A9F4").ToAndroid();
				decorView.SetBackground(new SplitDrawable(color, statusBarHeight, navigationBarHeight));
			}
		}

		private class SplitDrawable : Drawable
		{
			private readonly int _bottomSize;
			private readonly AColor _color;
			private readonly int _topSize;

			public SplitDrawable(AColor color, int topSize, int bottomSize)
			{
				_color = color;
				_bottomSize = bottomSize;
				_topSize = topSize;
			}

			public override int Opacity => (int)Format.Opaque;

			public override void Draw(Canvas canvas)
			{
				var bounds = Bounds;

				var paint = new Paint();

				paint.Color = _color;

				canvas.DrawRect(new Rect(0, 0, bounds.Right, _topSize), paint);

				canvas.DrawRect(new Rect(0, bounds.Bottom - _bottomSize, bounds.Right, bounds.Bottom), paint);
			}

			public override void SetAlpha(int alpha)
			{
			}

			public override void SetColorFilter(ColorFilter colorFilter)
			{
			}
		}

		#region IDisposable

		void IDisposable.Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;
					Element.SizeChanged -= OnElementSizeChanged;
				}

				Element = null;
				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				_disposed = true;
			}
		}

		#endregion IDisposable
	}
}