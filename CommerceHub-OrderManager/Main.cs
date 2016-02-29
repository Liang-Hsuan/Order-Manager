﻿using CommerceHub_OrderManager.channel.sears;
using System;
using System.Windows.Forms;

namespace CommerceHub_OrderManager
{
    public partial class Main : Form
    {
        // field for commerce hub order
        private Sears sears;

        public Main()
        {
            InitializeComponent();

            sears = new Sears();

            // show new orders from sears
            showSearsResult();

            // show result on the chart
            refreshGraph();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Date = DateTime.Today;
        }

        #region Top Buttons
        /* print button click that export the checked items' packing slip */
        private void printButton_Click(object sender, EventArgs e)
        {
            // initialize printing objects
            SearsPackingSlip searsPS = new SearsPackingSlip();

            // check the user check item and get the selected transaction id for exportin packing slip
            foreach (ListViewItem item in listview.CheckedItems)
            {
                // for sears order
                if (item.SubItems[0].Text == "Sears")
                {
                    string transaction = item.SubItems[4].Text;
                    SearsValues value = sears.generateValue(transaction);
                    searsPS.createPackingSlip(value, new int[0], true);
                }
            }
        }

        /* the event for refresh button clicks that refresh the order in listview and the chart */
        private void refreshButton_Click(object sender, EventArgs e)
        {
            showSearsResult();
            refreshGraph();
        }

        /* the event for detail button click that show the detail page for the selected item */
        private void detailButton_Click(object sender, EventArgs e)
        {
            // the case if the user does not select any thing or select more than one
            if (listview.CheckedItems.Count != 1)
            {
                MessageBox.Show("Please select one item to see more details", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SearsValues value = sears.generateValue(listview.CheckedItems[0].SubItems[4].Text);

            new DetailPage(value).ShowDialog(this);
        }
        #endregion

        /* the event for selection all checkbox check that select all the items in the list view */
        private void selectionAllCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (selectionAllCheckbox.Checked)
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

        #region Supporting Methods
        /* a supporting method that get new order from sears and show them on the list view */
        private void showSearsResult()
        {
            // first clear the listview
            listview.Items.Clear();

            // get orders from sears
            sears.GetOrder();
            SearsValues[] searsValue = sears.GetAllNewOrder();

            // show new orders to the list view 
            DateTime timeNow = DateTime.Now;
            foreach (SearsValues value in searsValue)
            {
                ListViewItem item = new ListViewItem("Sears");

                TimeSpan span = timeNow.Subtract(value.CustOrderDate);
                item.SubItems.Add(span.Days + "d " + span.Hours + "h");

                if (value.LineCount > 1)
                {
                    item.SubItems.Add("(Multiple Items)");
                    item.SubItems.Add("(Multiple Items)");
                }
                else
                {
                    item.SubItems.Add(value.Description[0]);
                    item.SubItems.Add(value.TrxVendorSKU[0]);
                }

                item.SubItems.Add(value.TransactionID);
                item.SubItems.Add(value.CustOrderDate.ToString("yyyy-MM-dd"));
                item.SubItems.Add(value.TrxBalanceDue.ToString());

                int total = 0;
                foreach (int qty in value.TrxQty)
                    total += qty;
                item.SubItems.Add(total.ToString());

                item.SubItems.Add(value.Recipient.Name);
                listview.Items.Add(item);
            }
        }

        /* a supporting method that refresh the chart */
        private void refreshGraph()
        {
            // clear chart first
            foreach (var series in chart.Series)
                series.Points.Clear();

            // creating chart
            DateTime from = DateTime.Today;
            for (int i = -6; i <= 0; i++)
            {
                from = DateTime.Today.AddDays(i);

                int order = sears.GetNumberOfOrder(from);
                int shipped = sears.GetNumberOfShipped(from);

                if (order < 1)
                {
                    chart.Series["orders"].Points.AddXY(from.ToString("MM/dd/yyyy"), 0);
                    chart.Series["point"].Points.AddXY(from.ToString("MM/dd/yyyy"), 0);
                    chart.Series["shipment"].Points.AddXY(from.ToString("MM/dd/yyyy"), 0);
                }
                else
                {
                    chart.Series["orders"].Points.AddXY(from.ToString("MM/dd/yyyy"), order);
                    chart.Series["point"].Points.AddXY(from.ToString("MM/dd/yyyy"), order);
                    chart.Series["shipment"].Points.AddXY(from.ToString("MM/dd/yyyy"), shipped);
                }
            }

            chart.Series["shipment"]["PointWidth"] = "0.1";
            chart.Series["point"].MarkerSize = 10;
        }
        #endregion
    }
}
