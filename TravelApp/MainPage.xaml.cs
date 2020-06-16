using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using SkiaSharp.Views;
using SkiaSharp;
using Xamarin.Essentials;

namespace TravelApp
{
    public partial class MainPage : ContentPage
    {
        private double density;
        private float padding;
        private float cutoutPosPx = 0;
        private float cutoutHeightPx;
        private double cutoutPos;
        private float cutoutHeigth = 250;
        private double expandValuePx;

        private enum State
        {
            Collapsed,
            Expanded
        }

        private State _currentState = State.Collapsed;

        public MainPage()
        {
            InitializeComponent();
            density = DeviceDisplay.MainDisplayInfo.Density;
            padding = 30 * (float)density;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // auto play the video since it won't auto render for some reason
            AutoPlay(.2);
        }

        private async void AutoPlay(double sec)
        {
            await Task.Delay((int)(sec * 1000));

            Video.Play();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // how big should my cutout be
            cutoutHeightPx = (float)(300 * density);
            cutoutPos = height - (cutoutHeigth + padding);
            cutoutPosPx = (float)(cutoutPos * density);
            Video.TranslationY = cutoutPos;
        }

        private SKPaint backgroundPaint = new SKPaint()
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        private void SkiaOverlay_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs e)
        {
            SKImageInfo info = e.Info;
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            float left = padding;
            float top = cutoutPosPx;
            float right = info.Width - padding;
            float bottom = cutoutPosPx + cutoutHeightPx;

            // adjust the cutout based on the expand animation
            if (expandValuePx > 0)
            {
                // change the values
                left -= (float)expandValuePx;
                if (left < 0) left = 0;

                right += (float)expandValuePx;
                if (right > info.Width) right = info.Width;

                top -= (float)expandValuePx;
                if (top < 0) top = 0;

                bottom += (float)expandValuePx;
                if (bottom > info.Height) bottom = info.Height;
            }

            // create a cutout
            var cutoutRect = new SKRect(left, top, right, bottom);
            canvas.ClipRoundRect(new SKRoundRect(cutoutRect, padding, padding), SKClipOperation.Difference, true);

            // draw the background
            canvas.DrawRect(new SKRect(0, 0, info.Width, info.Height), backgroundPaint);
        }

        private void TouchEff_Completed(VisualElement sender, TouchEffect.EventArgs.TouchCompletedEventArgs args)
        {
            // kick off the animation
            _currentState = _currentState == State.Collapsed ? State.Expanded : State.Collapsed;
            GoToState(_currentState);
        }

        private void GoToState(State _currentState)
        {
            double startPos = 0;
            double endPos = 0;
            double startExpand = 0;
            double endExpand = 0;

            if (_currentState == State.Collapsed)
            {
                startPos = 0;
                endPos = this.Height - (cutoutHeigth + padding);

                startExpand = this.Height;
                endExpand = 0;
            }
            else
            {
                endPos = 0;
                startPos = this.Height - (cutoutHeigth + padding);

                endExpand = this.Height;
                startExpand = 0;
            }

            // set the initial value
            cutoutPosPx = (float)(startPos * density);
            expandValuePx = startExpand * density;

            // animate cutoutPos
            Animation cutoutAnim = new Animation(t =>
            {
                // adjust the position of the cutout
                cutoutPosPx = (float)(t * density);

                // adjust position of video to match
                Video.TranslationY = t;

                // invalidate the canvas
                SkiaOverlay.InvalidateSurface();
            }, startPos, endPos, Easing.SinInOut);

            Animation expandAnim = new Animation(exp =>
            {
                // adjust the position of the cutout
                expandValuePx = exp * density;

                // invalidate the canvas
                SkiaOverlay.InvalidateSurface();
            }, startExpand, endExpand, Easing.SinInOut);

            Animation parentAnimation = new Animation();

            if (_currentState == State.Expanded)
            {
                parentAnimation.Add(0, .5, cutoutAnim);
                parentAnimation.Add(0.5, 1, expandAnim);
            }
            else
            {
                parentAnimation.Add(0, .5, expandAnim);
                parentAnimation.Add(0.5, 1, cutoutAnim);
            }
            parentAnimation.Commit(this, "ExpandAnimation", 16, 1000);
        }
    }
}