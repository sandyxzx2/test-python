namespace testSoulChat
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            startButton = new Button();
            stopButton = new Button();
            logTextBox = new TextBox();
            SuspendLayout();
            // 
            // startButton
            // 
            startButton.Location = new Point(22, 20);
            startButton.Name = "startButton";
            startButton.Size = new Size(110, 34);
            startButton.TabIndex = 0;
            startButton.Text = "启动助手";
            startButton.UseVisualStyleBackColor = true;
            startButton.Click += startButton_Click;
            // 
            // stopButton
            // 
            stopButton.Location = new Point(147, 20);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(110, 34);
            stopButton.TabIndex = 1;
            stopButton.Text = "停止助手";
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += stopButton_Click;
            // 
            // logTextBox
            // 
            logTextBox.Location = new Point(22, 68);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(756, 364);
            logTextBox.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(logTextBox);
            Controls.Add(stopButton);
            Controls.Add(startButton);
            Name = "Form1";
            Text = "自动社交助手";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button startButton;
        private Button stopButton;
        private TextBox logTextBox;
    }
}
