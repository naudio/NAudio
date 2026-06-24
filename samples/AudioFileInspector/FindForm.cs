using System;
using System.Windows.Forms;

namespace AudioFileInspector;

public partial class FindForm : Form
{
    private readonly RichTextBox richTextBox;
    public FindForm(RichTextBox richTextBox)
    {
        InitializeComponent();
        this.richTextBox = richTextBox;
    }

    private void buttonFind_Click(object sender, EventArgs e)
    {
        richTextBox.Find(textBoxFind.Text,
            richTextBox.SelectionStart +
            richTextBox.SelectionLength,
            RichTextBoxFinds.None);
        richTextBox.ScrollToCaret();
    }
}
