namespace GUI
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
            this.button_woc1 = new ePOSOne.btnProduct.Button_WOC();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.button_woc4 = new ePOSOne.btnProduct.Button_WOC();
            this.button_woc2 = new ePOSOne.btnProduct.Button_WOC();
            this.button_woc3 = new ePOSOne.btnProduct.Button_WOC();
            this.SuspendLayout();
            // 
            // button_woc1
            // 
            this.button_woc1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc1.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(30)))), ((int)(((byte)(58)))));
            this.button_woc1.FlatAppearance.BorderSize = 0;
            this.button_woc1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_woc1.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.button_woc1.Location = new System.Drawing.Point(528, 69);
            this.button_woc1.Name = "button_woc1";
            this.button_woc1.OnHoverBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc1.OnHoverButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc1.OnHoverTextColor = System.Drawing.SystemColors.HighlightText;
            this.button_woc1.Size = new System.Drawing.Size(201, 65);
            this.button_woc1.TabIndex = 1;
            this.button_woc1.Text = "Add";
            this.button_woc1.TextColor = System.Drawing.Color.White;
            this.button_woc1.UseVisualStyleBackColor = true;
            this.button_woc1.Click += new System.EventHandler(this.Add_Click);
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBox1.Location = new System.Drawing.Point(94, 85);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(360, 35);
            this.textBox1.TabIndex = 2;
            this.textBox1.Click += new System.EventHandler(this.none);
            this.textBox1.Enter += new System.EventHandler(this.Form1_Load);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(91, 159);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(360, 296);
            this.richTextBox1.TabIndex = 3;
            this.richTextBox1.Text = "";
            this.richTextBox1.WordWrap = false;
            this.richTextBox1.TextChanged += new System.EventHandler(this.Rtb_TextChanged);
            // 
            // button_woc4
            // 
            this.button_woc4.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc4.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(30)))), ((int)(((byte)(58)))));
            this.button_woc4.FlatAppearance.BorderSize = 0;
            this.button_woc4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_woc4.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.button_woc4.Location = new System.Drawing.Point(528, 386);
            this.button_woc4.Name = "button_woc4";
            this.button_woc4.OnHoverBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc4.OnHoverButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc4.OnHoverTextColor = System.Drawing.SystemColors.HighlightText;
            this.button_woc4.Size = new System.Drawing.Size(201, 65);
            this.button_woc4.TabIndex = 1;
            this.button_woc4.Text = "Open textfile";
            this.button_woc4.TextColor = System.Drawing.Color.White;
            this.button_woc4.UseVisualStyleBackColor = true;
            this.button_woc4.Click += new System.EventHandler(this.Openfile_Click);
            // 
            // button_woc2
            // 
            this.button_woc2.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc2.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(30)))), ((int)(((byte)(58)))));
            this.button_woc2.FlatAppearance.BorderSize = 0;
            this.button_woc2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_woc2.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.button_woc2.Location = new System.Drawing.Point(528, 178);
            this.button_woc2.Name = "button_woc2";
            this.button_woc2.OnHoverBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc2.OnHoverButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc2.OnHoverTextColor = System.Drawing.SystemColors.HighlightText;
            this.button_woc2.Size = new System.Drawing.Size(201, 65);
            this.button_woc2.TabIndex = 1;
            this.button_woc2.Text = "Run line";
            this.button_woc2.TextColor = System.Drawing.Color.White;
            this.button_woc2.UseVisualStyleBackColor = true;
            this.button_woc2.Click += new System.EventHandler(this.Runline_Click);
            // 
            // button_woc3
            // 
            this.button_woc3.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc3.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(30)))), ((int)(((byte)(58)))));
            this.button_woc3.FlatAppearance.BorderSize = 0;
            this.button_woc3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_woc3.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.button_woc3.Location = new System.Drawing.Point(526, 278);
            this.button_woc3.Name = "button_woc3";
            this.button_woc3.OnHoverBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc3.OnHoverButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.button_woc3.OnHoverTextColor = System.Drawing.SystemColors.HighlightText;
            this.button_woc3.Size = new System.Drawing.Size(201, 65);
            this.button_woc3.TabIndex = 1;
            this.button_woc3.Text = "Run all";
            this.button_woc3.TextColor = System.Drawing.Color.White;
            this.button_woc3.UseVisualStyleBackColor = true;
            this.button_woc3.Click += new System.EventHandler(this.Runall_Click);
            // 
            // Form1
            // 
            this.AcceptButton = this.button_woc1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(30)))), ((int)(((byte)(58)))));
            this.ClientSize = new System.Drawing.Size(800, 495);
            this.Controls.Add(this.button_woc3);
            this.Controls.Add(this.button_woc2);
            this.Controls.Add(this.button_woc4);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button_woc1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "run command";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private ePOSOne.btnProduct.Button_WOC button_woc1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private ePOSOne.btnProduct.Button_WOC button_woc4;
        private ePOSOne.btnProduct.Button_WOC button_woc2;
        private ePOSOne.btnProduct.Button_WOC button_woc3;
    }
}

