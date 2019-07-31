using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CameraInWPF
{
    /// <summary>
    /// FaceTrackingVisualizationCanvas.xaml 的交互逻辑
    /// </summary>
    public partial class RealTimeFaceIdentificationBorder : UserControl
    {
        public RealTimeFaceIdentificationBorder()
        {
            InitializeComponent();
        }


        public void ShowFaceRectangle(double left, double top, double width, double height)
        {
            this.faceRectangle.Margin = new Thickness(left, top, 0, 0);
            this.faceRectangle.Width = width;
            this.faceRectangle.Height = height;

            this.faceRectangle.Visibility = Visibility.Visible;
        }

        public void ShowIdentificationData(string name = null)
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.captionTextHeader.Text = name;
            }

            this.captionBorder.Visibility = Visibility.Visible;
            this.captionBorder.Margin = new Thickness(this.faceRectangle.Margin.Left - (this.captionBorder.Width - this.faceRectangle.Width) / 2,
                                                    this.faceRectangle.Margin.Top - this.captionBorder.Height - 2, 0, 0);
        }
    }
}
