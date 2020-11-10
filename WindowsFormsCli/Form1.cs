using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace WindowsFormsCli
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.textBoxURL.Text = "https://localhost:5001";
        }

        private HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.textBoxURL.Text);
            return client;
        }

        private async void buttonFill_Click(object sender, EventArgs e)
        {
            try
            {
                using (HttpClient client = CreateClient())
                {
                    Cursor.Current = Cursors.WaitCursor;
                    var response = await client.GetAsync("/api/dokumentumok");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var res = await response.Content.ReadAsStringAsync();
                        var files = (JsonSerializer.Deserialize(res, typeof(string[])) as string[]).ToList();
                        this.listBox1.DataSource = files;
                    }
                    else
                    {
                        MessageBox.Show(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void buttonGet_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                try
                {
                    using (HttpClient client = CreateClient())
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        var name = HttpUtility.UrlEncode(listBox1.SelectedItem.ToString());
                        var response = await client.GetAsync($"/api/dokumentumok/{name}");
                        var res = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var data = Convert.FromBase64String(res);
                            if (data.Length > 1E4)
                            {
                                res = "A file túl nagy megjeleníteni.";
                            }
                            else
                            {
                                res = Encoding.UTF8.GetString(data);
                            }
                        }
                        MessageBox.Show(res);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void buttonSet_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    var fileContent = string.Empty;
                    var filePath = string.Empty;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        var data = await System.IO.File.ReadAllBytesAsync(openFileDialog.FileName);
                        using (HttpClient client = CreateClient())
                        {
                            string body = Convert.ToBase64String(data);
                            var content = new StringContent(body);
                            var name = HttpUtility.UrlEncode(Path.GetFileName(openFileDialog.FileName));
                            var response = await client.PostAsync($"/api/dokumentumok/{name}", content);
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                var err = await response.Content.ReadAsStringAsync();
                                MessageBox.Show(err);
                            }
                            else
                            {
                                MessageBox.Show("Upload OK");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
