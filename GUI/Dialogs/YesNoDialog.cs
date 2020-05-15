using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace CKAN
{
    public partial class YesNoDialog : Form
    {
        public YesNoDialog()
        {
            InitializeComponent();
            defaultYes = YesButton.Text;
            defaultNo  = NoButton.Text;
        }

        public DialogResult ShowYesNoDialog(Form parentForm, string text, string yesText = null, string noText = null)
        {
            task = new TaskCompletionSource<DialogResult>();

            Util.Invoke(parentForm, () =>
            {
                var height = StringHeight(text, ClientSize.Width - 25) + 2 * 54;
                DescriptionLabel.Text = text;
                DescriptionLabel.TextAlign = text.Contains("\n")
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Center;
                DescriptionLabel.ScrollBars = height < maxHeight
                    ? ScrollBars.None
                    : ScrollBars.Vertical;
                YesButton.Text = yesText ?? defaultYes;
                NoButton.Text  = noText  ?? defaultNo;
                ClientSize = new Size(
                    ClientSize.Width,
                    Math.Min(maxHeight, height)
                );
                task.SetResult(ShowDialog(parentForm));
            });

            return task.Task.Result;
        }

        /// <summary>
        /// Simple syntactic sugar around Graphics.MeasureString
        /// </summary>
        /// <param name="text">String to measure size of</param>
        /// <param name="maxWidth">Number of pixels allowed horizontally</param>
        /// <returns>
        /// Number of pixels needed vertically to fit the string
        /// </returns>
        private int StringHeight(string text, int maxWidth)
        {
            return (int)CreateGraphics().MeasureString(text, DescriptionLabel.Font, maxWidth).Height;
        }

        public void HideYesNoDialog()
        {
            Util.Invoke(this, Close);
        }

        private const int maxHeight = 600;
        private TaskCompletionSource<DialogResult> task;
        private string defaultYes;
        private string defaultNo;
    }
}
