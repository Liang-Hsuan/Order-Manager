﻿using CommerceHub_OrderManager.channel.sears;
using CommerceHub_OrderManager.supportingClasses;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace CommerceHub_OrderManager
{
    public partial class DetailPage : Form
    {
        // field for storing order details
        private SearsValues value;

        // field for keeping track cancelled items
        private Dictionary<int, string> cancalList;

        /* constructor that initializes graphic compents and order fields */
        public DetailPage(SearsValues value)
        {
            InitializeComponent();

            this.value = value;
            showResult(value);
        }

        /* print packing slip button clicks that print the packing slip for the order item(s) */
        private void printPackingSlipButton_Click(object sender, EventArgs e)
        {
            // get all cancel index and print the packing slip that are not cancelled
            int[] cancelIndex = getCancelIndex();
            SearsPackingSlip packingSlip = new SearsPackingSlip();
            packingSlip.createPackingSlip(value, cancelIndex, true);
        }

        /* the event for verify button click that show the result of the address validity */
        private void verifyButton_Click(object sender, EventArgs e)
        {
            bool flag = new AddressValidation().validate(value.ShipTo);

            if (flag)
            {
                verifyTextbox.Text = "Address Verified";
                verifyTextbox.ForeColor = Color.FromArgb(100, 168, 17);
            }
            else
            {
                verifyTextbox.Text = "Address Not Valid";
                verifyTextbox.ForeColor = Color.FromArgb(254, 126, 116);
            }

            verifyTextbox.Visible = true;
        }

        #region ListView
        /* the event for select all checkbox check change that select all checkbox all deselect all */
        private void selectAllCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (selectAllCheckbox.Checked)
            {
                for (int i = 0; i < listview.Items.Count; i++)
                    listview.Items[i].Checked = true;
            }
            else
            {
                for (int i = 0; i < listview.Items.Count; i++)
                    listview.Items[i].Checked = false;
            }
        }

        /* the event for mark as cancel button click that mark the checked item to cancelled shipment */
        private void markCancelButton_Click(object sender, EventArgs e)
        {
            int count = 0;

            foreach (ListViewItem item in listview.Items)
            {
                if (item.Checked)
                {
                    item.SubItems[5].Text = "Cancelled";
                    count++;
                }
                else
                {
                    item.SubItems[5].Text = "";
                    item.SubItems[6].Text = "";
                }

            }

            if (count > 0)
            {
                listview.Columns[6].Text = "Reason";
                reasonCombobox.Visible = true;
                setReasonButton.Visible = true;
            }
            else
            {
                listview.Columns[6].Text = "";
                reasonCombobox.Visible = false;
                setReasonButton.Visible = false;
            }

            // check the number of the cancel items compare to those are not so that change the enability of the print button
            if (count >= listview.Items.Count)
                printPackingSlipButton.Enabled = false;
            else
                printPackingSlipButton.Enabled = true;
        }

        /* when clicked set the reason of cancelling to the checked items */
        private void setReasonButton_Click(object sender, EventArgs e)
        {
            if (reasonCombobox.SelectedIndex != 0)
            {
                foreach (ListViewItem item in listview.CheckedItems)
                {
                    item.SubItems[6].Text = reasonCombobox.SelectedItem.ToString();
                }
            }
        }

        /* prevent user from changing header size */
        private void listview_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listview.Columns[e.ColumnIndex].Width;
        }
        #endregion

        private void createLabelButton_Click(object sender, EventArgs e)
        {
            // ask for user confirmaiton
            ConfirmPanel confirm = new ConfirmPanel("Are you sure you want to ship the package ?");
            confirm.ShowDialog(this);

            if (confirm.DialogResult == DialogResult.OK)
            {
                // initialize fields for shipment
                Package package = new Package(weightKgUpdown.Value, lengthUpdown.Value, widthUpdown.Value, heightUpdown.Value, serviceCombobox.SelectedItem.ToString());
                UPS ups = new UPS();

                string digest = ups.postShipmentConfirm(value, package);
            }
        }

        #region Shipment Confirm
        /* shipment confirm button clicks that send the confirm xml to sears */
        private void shipmentConfirmButton_Click(object sender, EventArgs e)
        {
            // get user's confirmation
            ConfirmPanel confirm = new ConfirmPanel("Are you sure you want to ship this order ?");
            confirm.ShowDialog(this);

            if (confirm.DialogResult == DialogResult.OK)
            {
                // generate cancel list
                cancalList = new Dictionary<int, string>();
                for (int i = 0; i < listview.Items.Count; i++)
                {
                    if (listview.Items[i].SubItems[5].Text == "Cancelled")
                    {
                        string reason = listview.Items[i].SubItems[6].Text;

                        // the case if the user has not provide the reason for cancelling a item
                        if (reason == "")
                        {
                            MessageBox.Show("Please provide the reason of cancellation", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        cancalList.Add(i, reason);
                    }
                }

                progressbar.Visible = true;

                // call background worker
                if (!backgroundWorkerConfirm.IsBusy)
                {
                    backgroundWorkerConfirm.RunWorkerAsync();
                }
            }
        }
        private void backgroundWorkerConfirm_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            simulate(1, 50);

            // export xml file
            new Sears().generateXML(value, cancalList);

            simulate(50, 100);
        }
        private void backgroundWorkerConfirm_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressbar.Value = e.ProgressPercentage;
        }
        private void backgroundWorkerConfirm_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            progressbar.Visible = false;
            shipmentConfirmButton.Text = "Completed";
            shipmentConfirmButton.BackColor = Color.Transparent;
            shipmentConfirmButton.Enabled = false;
        }
        #endregion

        #region Supporting Region
        /* a method that show the information of the given SearsValues object */
        private void showResult(SearsValues value)
        {
            topOrderNumberTextbox.Text = value.TransactionID;

            #region Order Summary
            // date
            orderDateTextbox.Text = value.CustOrderDate.ToString("MM/dd/yyyy");
            paidDateTextbox.Text = value.CustOrderDate.ToString("MM/dd/yyyy");
            shipByDateTextbox.Text = value.ExpectedShipDate[0].ToString("MM/dd/yyyy");

            // unit price
            double price = 0;
            foreach (double unitPrice in value.UnitPrice)
                price += unitPrice;
            unitPriceTotalTextbox.Text = price.ToString();

            // GST and HST
            price = 0;
            foreach (double gstHst in value.GST_HST_Extended)
                price += gstHst;
            foreach (double gstHst in value.GST_HST_Total)
                price += gstHst;
            gsthstTextbox.Text = price.ToString();

            // PST
            price = 0;
            foreach (double pst in value.PST_Extended)
                price += pst;
            foreach (double pst in value.PST_Total)
                price += pst;
            pstTextbox.Text = price.ToString();

            // other fee
            price = 0;
            foreach (double fee in value.LineHandling)
                price += fee;
            otherFeeTextbox.Text = price.ToString();

            // total 
            totalOrderTextbox.Text = value.TrxBalanceDue.ToString();
            #endregion

            #region Buyer / Recipient Info
            // sold to 
            soldToTextbox.Text = value.Recipient.Name;
            soldToPhoneTextbox.Text = value.Recipient.DayPhone;

            // ship to
            shipToNameTextbox.Text = value.ShipTo.Name;
            shipToAddress1Textbox.Text = value.ShipTo.Address1;
            shipToAddress2Textbox.Text = value.ShipTo.Address2;
            shipToCombineTextbox.Text = value.ShipTo.City + ", " + value.ShipTo.State + ", " + value.ShipTo.PostalCode;
            shipToPhoneTextbox.Text = value.ShipTo.DayPhone;
            #endregion

            #region Listview and Shipping Info
            // ups details
            switch (value.ServiceLevel)
            {
                case "UPSN_SE":
                    serviceCombobox.SelectedIndex = 0;
                    break;
                case "UPSN_3D":
                    serviceCombobox.SelectedIndex = 1;
                    break;
                case "UPSN_ND":
                    serviceCombobox.SelectedIndex = 3;
                    break;
                default:
                    serviceCombobox.SelectedIndex = 2;
                    break;
            }

            // initialize field for sku detail -> [0] weight, [1] length, [2] width, [3] height
            decimal[] skuDetail = { 0, 0, 0, 0 };

            // adding list to listview and getting sku detail
            for (int i = 0; i < value.LineCount; i++)
            {
                // add item to list
                ListViewItem item = new ListViewItem(value.MerchantLineNumber[i].ToString());

                item.SubItems.Add(value.Description[i] + "  SKU: " + value.TrxVendorSKU[i]);
                item.SubItems.Add("$ " + value.UnitPrice[i]);
                item.SubItems.Add(value.TrxQty[i].ToString());
                item.SubItems.Add("$ " + value.LineBalanceDue[i]);
                item.SubItems.Add("");
                item.SubItems.Add("");

                listview.Items.Add(item);

                // generate sku detail
                decimal[] detailList = getSkuDetail(value.TrxVendorSKU[i]);

                // the case if bad sku
                if (detailList == null)
                    item.BackColor = Color.FromArgb(254, 126, 116);
                else
                {
                    for (int j = 0; j < 4; j++)
                        skuDetail[j] += detailList[j];
                }
            }

            // show result to shipping info
            weightKgUpdown.Value = skuDetail[0] / 1000;
            weightLbUpdown.Value = skuDetail[0] / 453.592m;
            lengthUpdown.Value = skuDetail[1];
            widthUpdown.Value = skuDetail[2];
            heightUpdown.Value = skuDetail[3];
            #endregion
        }

        /* a method that get the detail of the given sku */
        private decimal[] getSkuDetail(string sku)
        {
            // local supporting fields
            decimal[] list = new decimal[4];

            // [0] weight, [1] length, [2] width, [3] height
            using (SqlConnection conneciton = new SqlConnection(Properties.Settings.Default.Designcs))
            {
                SqlCommand command = new SqlCommand("SELECT Weight_grams, Shippable_Depth_cm, Shippable_Width_cm, Shippable_Height_cm " +
                                                    "FROM master_Design_Attributes design JOIN master_SKU_Attributes sku ON (design.Design_Service_Code = sku.Design_Service_Code) " + 
                                                    "WHERE SKU_Ashlin = \'" + sku + "\';", conneciton);
                conneciton.Open();
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();

                // check if there is result
                if (!reader.HasRows)
                    return null;

                for (int i = 0; i < 4; i++)
                    list[i] = Convert.ToDecimal(reader.GetValue(i));
            }

            return list;
        }

        /* a method that get the current cancel items' idexes */
        private int[] getCancelIndex()
        {
            List<int> list = new List<int>();

            foreach (ListViewItem item in listview.Items)
            {
                if (item.SubItems[5].Text == "Cancelled")
                    list.Add(item.Index);
            }

            return list.ToArray();
        }

        /* a method that report to progress bar value from the start to end */
        private void simulate(int start, int end)
        {
            // simulate progress 1% ~ 30%
            for (int i = start; i <= end; i++)
            {
                Thread.Sleep(30);
                backgroundWorkerConfirm.ReportProgress(i);
            }
        }
        #endregion
    }
}
