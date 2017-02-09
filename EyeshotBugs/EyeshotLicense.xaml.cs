using System.Windows;

namespace EyeshotBugs
{

    public partial class PromptDialog : Window
    {
        public enum InputType
        {
            Text,
            Password
        }

        private InputType _inputType = InputType.Text;

        public PromptDialog(string question, string title, string defaultValue = "", InputType inputType = InputType.Text)
        {
            InitializeComponent();
            Loaded += PromptDialog_Loaded;
            txtQuestion.Text = question;
            Title = title;
            txtResponse.Text = defaultValue;
            _inputType = inputType;
            if (_inputType == InputType.Password)
                txtResponse.Visibility = Visibility.Collapsed;
            else
                txtPasswordResponse.Visibility = Visibility.Collapsed;
        }

        void PromptDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (_inputType == InputType.Password)
                txtPasswordResponse.Focus();
            else
                txtResponse.Focus();
        }

        public static string Prompt(string question, string title, string defaultValue = "", InputType inputType = InputType.Text)
        {
            PromptDialog inst = new PromptDialog(question, title, defaultValue, inputType);
            inst.ShowDialog();
            if (inst.DialogResult == true)
                return inst.ResponseText;
            return null;
        }

        public string ResponseText
        {
            get
            {
                if (_inputType == InputType.Password)
                    return txtPasswordResponse.Password;
                return txtResponse.Text;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

}
