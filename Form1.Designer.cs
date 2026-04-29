namespace RobotLocalization
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lidarPictureBox = new System.Windows.Forms.PictureBox();
            this.portTextBox = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.startButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.mapPictureBox = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.reportListBox = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.pointsListBox = new System.Windows.Forms.ListBox();
            this.moveButton = new System.Windows.Forms.Button();
            this.LocalPortTextBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.RemotePortTextBox = new System.Windows.Forms.TextBox();
            this.RemoteIPTextBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.lidarPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.portTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mapPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // lidarPictureBox
            // 
            this.lidarPictureBox.Location = new System.Drawing.Point(14, 260);
            this.lidarPictureBox.Margin = new System.Windows.Forms.Padding(2);
            this.lidarPictureBox.Name = "lidarPictureBox";
            this.lidarPictureBox.Size = new System.Drawing.Size(448, 422);
            this.lidarPictureBox.TabIndex = 0;
            this.lidarPictureBox.TabStop = false;
            // 
            // portTextBox
            // 
            this.portTextBox.Location = new System.Drawing.Point(113, 27);
            this.portTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.portTextBox.Maximum = new decimal(new int[] {
            15000,
            0,
            0,
            0});
            this.portTextBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(75, 20);
            this.portTextBox.TabIndex = 13;
            this.portTextBox.Value = new decimal(new int[] {
            2368,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(110, 9);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Порт лидаров";
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(472, 22);
            this.startButton.Margin = new System.Windows.Forms.Padding(2);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(126, 23);
            this.startButton.TabIndex = 18;
            this.startButton.Text = "Подключиться";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            // 
            // mapPictureBox
            // 
            this.mapPictureBox.Location = new System.Drawing.Point(471, 260);
            this.mapPictureBox.Margin = new System.Windows.Forms.Padding(2);
            this.mapPictureBox.Name = "mapPictureBox";
            this.mapPictureBox.Size = new System.Drawing.Size(448, 422);
            this.mapPictureBox.TabIndex = 19;
            this.mapPictureBox.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 245);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Данные лидара";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(468, 245);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Карта";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(469, 71);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "Точки интереса";
            // 
            // reportListBox
            // 
            this.reportListBox.FormattingEnabled = true;
            this.reportListBox.HorizontalScrollbar = true;
            this.reportListBox.Location = new System.Drawing.Point(14, 87);
            this.reportListBox.Name = "reportListBox";
            this.reportListBox.ScrollAlwaysVisible = true;
            this.reportListBox.Size = new System.Drawing.Size(447, 121);
            this.reportListBox.TabIndex = 23;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 71);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(26, 13);
            this.label5.TabIndex = 24;
            this.label5.Text = "Лог";
            // 
            // pointsListBox
            // 
            this.pointsListBox.FormattingEnabled = true;
            this.pointsListBox.HorizontalScrollbar = true;
            this.pointsListBox.Location = new System.Drawing.Point(472, 87);
            this.pointsListBox.Name = "pointsListBox";
            this.pointsListBox.ScrollAlwaysVisible = true;
            this.pointsListBox.Size = new System.Drawing.Size(447, 121);
            this.pointsListBox.TabIndex = 25;
            this.pointsListBox.SelectedIndexChanged += new System.EventHandler(this.pointsListBox_SelectedIndexChanged);
            // 
            // moveButton
            // 
            this.moveButton.Location = new System.Drawing.Point(472, 213);
            this.moveButton.Margin = new System.Windows.Forms.Padding(2);
            this.moveButton.Name = "moveButton";
            this.moveButton.Size = new System.Drawing.Size(447, 23);
            this.moveButton.TabIndex = 26;
            this.moveButton.Text = "Отправить робота в выбранную точку";
            this.moveButton.UseVisualStyleBackColor = true;
            this.moveButton.Click += new System.EventHandler(this.moveButton_Click);
            // 
            // LocalPortTextBox
            // 
            this.LocalPortTextBox.Location = new System.Drawing.Point(324, 25);
            this.LocalPortTextBox.Name = "LocalPortTextBox";
            this.LocalPortTextBox.Size = new System.Drawing.Size(73, 20);
            this.LocalPortTextBox.TabIndex = 20;
            this.LocalPortTextBox.Text = "7777";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(321, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(91, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Локальный порт";
            // 
            // RemotePortTextBox
            // 
            this.RemotePortTextBox.Location = new System.Drawing.Point(221, 26);
            this.RemotePortTextBox.Name = "RemotePortTextBox";
            this.RemotePortTextBox.Size = new System.Drawing.Size(74, 20);
            this.RemotePortTextBox.TabIndex = 16;
            this.RemotePortTextBox.Text = "8888";
            // 
            // RemoteIPTextBox
            // 
            this.RemoteIPTextBox.Location = new System.Drawing.Point(14, 25);
            this.RemoteIPTextBox.Name = "RemoteIPTextBox";
            this.RemoteIPTextBox.Size = new System.Drawing.Size(74, 20);
            this.RemoteIPTextBox.TabIndex = 15;
            this.RemoteIPTextBox.Text = "127.0.0.1";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(218, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(70, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Порт робота";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 9);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(78, 13);
            this.label9.TabIndex = 13;
            this.label9.Text = "Удаленный IP";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 693);
            this.Controls.Add(this.LocalPortTextBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.moveButton);
            this.Controls.Add(this.pointsListBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.RemoteIPTextBox);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.RemotePortTextBox);
            this.Controls.Add(this.reportListBox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.mapPictureBox);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.lidarPictureBox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Кожевников Иван 231-328 ЛР6";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.lidarPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.portTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mapPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox lidarPictureBox;
        private System.Windows.Forms.NumericUpDown portTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.PictureBox mapPictureBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox reportListBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox pointsListBox;
        private System.Windows.Forms.Button moveButton;
        private System.Windows.Forms.TextBox LocalPortTextBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox RemotePortTextBox;
        private System.Windows.Forms.TextBox RemoteIPTextBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
    }
}

