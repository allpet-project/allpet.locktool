using Newtonsoft.Json.Linq;
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

namespace allpet.locktool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.listDates.Items.Add(new UTC_Time(2019, 5, 26, 15));
            this.listDates.Items.Add(new UTC_Time(2019, 7, 26, 15));
            this.listDates.Items.Add(new UTC_Time(2019, 9, 26, 15));
            this.listDates.Items.Add(new UTC_Time(2019, 11, 26, 15));
            this.listDates.Items.Add(new UTC_Time(2020, 1, 26, 15));
        }
        const string id_GAS = "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";
        const string id_PET = "0x959474606d539e13970c61d21103cc4bd955a214";
        const string lockscript = "56c56b6c766b00527ac46c766b51527ac46c766b52527ac4616168184e656f2e426c6f636b636861696e2e4765744865696768746168184e656f2e426c6f636b636861696e2e4765744865616465726c766b53527ac46c766b00c36c766b53c36168174e656f2e4865616465722e47657454696d657374616d70a06c766b54527ac46c766b54c3640e00006c766b55527ac4621a006c766b51c36c766b52c3617cac6c766b55527ac46203006c766b55c3616c7566";
        const string api = "https://api.nel.group/api/mainnet";
        public byte[] main_pubkey;
        public byte[] main_prikey;

        class Asset
        {
            public bool isnep5;
            public string assetID;
            public double balance;
            public string name;
            public int decimals = 0;
            public override string ToString()
            {
                return (isnep5 ? "<NEP5>" : "<UTXO>") + name + "=" + balance;
            }
        }
        async void UpdateMainInfo()
        {
            listMain.Items.Clear();
            listMain.Items.Add("pubkey=" + ThinNeo.Helper.Bytes2HexString(main_pubkey));
            var addr = ThinNeo.Helper.GetAddressFromPublicKey(main_pubkey);
            listMain.Items.Add("address=" + addr);

            //fill utxo balance;
            var url = Demo.Helper.MakeRpcUrlPost(api, "getbalance", out byte[] data, new MyJson.JsonNode_ValueString(addr));
            var json = await Demo.Helper.HttpPost(url, data);
            var jsonb = Newtonsoft.Json.Linq.JObject.Parse(json);
            var result = jsonb["result"] as JArray;
            var hasGas = false;
            if (result != null)
            {
                foreach (JObject r in result)
                {
                    var asset = new Asset();
                    asset.isnep5 = false;
                    asset.assetID = (string)r["asset"];
                    asset.balance = (double)r["balance"];
                    foreach (JObject nameitem in r["name"] as JArray)
                    {
                        asset.name = nameitem["name"].ToString();
                    }
                    if (asset.assetID == id_GAS)
                    {
                        asset.name = "::GAS";
                        hasGas = true;
                    }
                    listMain.Items.Add(asset);
                    var url2 = Demo.Helper.MakeRpcUrlPost(api, "getasset", out byte[] data2, new MyJson.JsonNode_ValueString(asset.assetID));
                    var json2 = await Demo.Helper.HttpPost(url2, data2);
                    var jsonb2 = Newtonsoft.Json.Linq.JObject.Parse(json2);

                    asset.decimals = (int)jsonb2["result"][0]["precision"];
                }
            }
        }
        async void InitMainKey(byte[] pubkey, byte[] prikey = null)
        {
            if (pubkey.Length != 33)
            {

                MessageBox.Show("error pub key");
                return;
            }
            this.main_pubkey = pubkey;
            UpdateMainInfo();

            this.listLocks.Items.Clear();
            foreach (UTC_Time d in this.listDates.Items)
            {
                await SetLockAccount(d);
            }

        }
        async Task SetLockAccount(UTC_Time time)
        {
            //lockscript;
            var lockbin = ThinNeo.Helper.HexString2Bytes(lockscript);
            var hash = ThinNeo.Helper.GetScriptHashFromScript(lockbin);
            var hashstr = hash.ToString();

            //生成脚本
            var timestamp = time.ToTimestamp();
            byte[] script;
            //on genbutton
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                sb.EmitPushBytes(main_pubkey); //sb.EmitPush(GetKey().PublicKey);

                sb.EmitPushNumber(timestamp);//sb.EmitPush(timestamp);

                //// Lock 2.0 in mainnet tx:4e84015258880ced0387f34842b1d96f605b9cc78b308e1f0d876933c2c9134b
                sb.EmitAppCall(hash); // script hash  = d3cce84d0800172d09c88ccad61130611bd047a4
                //return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
                script = sb.ToArray();
            }


            var callscripthash = ThinNeo.Helper.GetScriptHashFromScript(script);
            var contractaddr = ThinNeo.Helper.GetAddressFromScriptHash(callscripthash);
            string outline = time.Time_UTC.ToLongDateString() + " : " + contractaddr + "  PET=";



            {//fill nep5 balance.
                var urlb = Demo.Helper.MakeRpcUrlPost(api, "getnep5balanceofaddress", out byte[] databsub, new MyJson.JsonNode_ValueString(id_PET), new MyJson.JsonNode_ValueString(contractaddr));
                var jsonbsub = await Demo.Helper.HttpPost(urlb, databsub);
                var jsonbsubb = Newtonsoft.Json.Linq.JObject.Parse(jsonbsub);
                outline += (double)jsonbsubb["result"][0]["nep5balance"];

                //var url2 = Demo.Helper.MakeRpcUrlPost(api, "getallnep5assetofaddress", out byte[] data2, new MyJson.JsonNode_ValueString(contractaddr));
                //var json = await Demo.Helper.HttpPost(url2, data2);
                //var jsonb = Newtonsoft.Json.Linq.JObject.Parse(json);
                //var result = (jsonb["result"] as JArray);
                //if (result != null)
                //{
                //    foreach (JObject item in result)
                //    {
                //        var asset = new Asset();
                //        asset.isnep5 = true;
                //        asset.assetID = (string)item["assetid"];
                //        var urlsub = Demo.Helper.MakeRpcUrlPost(api, "getnep5asset", out byte[] datasub, new MyJson.JsonNode_ValueString(asset.assetID));
                //        var jsonsub = await Demo.Helper.HttpPost(urlsub, datasub);
                //        var jsonsubb = Newtonsoft.Json.Linq.JObject.Parse(jsonsub);
                //        asset.name = (string)jsonsubb["result"][0]["name"];
                //        asset.decimals = (int)jsonsubb["result"][0]["decimals"];

                //        var urlb = Demo.Helper.MakeRpcUrlPost(api, "getnep5balanceofaddress", out byte[] databsub, new MyJson.JsonNode_ValueString(asset.assetID), new MyJson.JsonNode_ValueString(contractaddr));
                //        var jsonbsub = await Demo.Helper.HttpPost(urlb, databsub);
                //        var jsonbsubb = Newtonsoft.Json.Linq.JObject.Parse(jsonbsub);
                //        asset.balance = (double)jsonbsubb["result"][0]["nep5balance"];

                //    }
                //}
            }

            this.listLocks.Items.Add(outline);
            var link = new Button();
            link.Width = 100;
            link.Height = 20;
            link.Content = "show in browser";
            link.Click += (s,e) =>
              {
                  System.Diagnostics.Process.Start("https://neotracker.io/address/"+ contractaddr);
              };
            this.listLocks.Items.Add(link);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var str_pubkey = Dialog_Input.ShowDialog(this, "input publickey here.");
            if(string.IsNullOrEmpty(str_pubkey))
            {
                MessageBox.Show("error key set.");
                return;
            }
            var pubkey = ThinNeo.Helper.HexString2Bytes(str_pubkey);
            InitMainKey(pubkey);


        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (main_pubkey == null)
                return;
            UpdateMainInfo();

            this.listLocks.Items.Clear();
            foreach (UTC_Time d in this.listDates.Items)
            {
                await SetLockAccount(d);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var prikey = Dialog_Import_Nep6.ShowDialog(this);
            if(prikey==null||prikey.Length==0)
            {
                MessageBox.Show("error key set.");
                return;
            }
            var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            InitMainKey(pubkey, prikey);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("not add this function yet.");
        }
    }
}
