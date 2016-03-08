﻿using CommerceHub_OrderManager.channel.sears;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace CommerceHub_OrderManager.supportingClasses
{
    /* 
     * A class that post shipment to UPS
     */
    public class UPS
    {
        // field for credentials
        private const string ACCESS_LISCENSE_NUMBER = "0D03B5751F524086";
        private const string USER_ID = "leonmaandbee";
        private const string PASSWORD = "Whatthefuck630";
        private const string ACCOUNT_NUMBER = "15XR35";
        private const string SEARS_ACCOUNT_NUMBER = "A9725A";

        // field for save image path
        private string savePathSears = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Sears_ShippingLabel";

        #region Posting Methods
        /* a method that post shipment confirm request and return shipment digest and identification number*/
        public string[] postShipmentConfirm(SearsValues value, Package package)
        {
            string shipmentConfirmUri = "https://wwwcie.ups.com/ups.app/xml/ShipConfirm";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(shipmentConfirmUri);
            request.Method = "POST";
            request.ContentType = "application/xml";

            string textXML =
                "<?xml version=\"1.0\"?>" +
                "<AccessRequest xml:lang=\"en-US\">" +
                "<AccessLicenseNumber>" + ACCESS_LISCENSE_NUMBER + "</AccessLicenseNumber>" +
                "<UserId>" + USER_ID + "</UserId>" +
                "<Password>" + PASSWORD + "</Password>" +
                "</AccessRequest>" +
                "<?xml version=\"1.0\"?>" +
                "<ShipmentConfirmRequest xml:lang=\"en-U\">" +
                "<Request>" +
                "<RequestAction>ShipConfirm</RequestAction>" +
                "<RequestOption>validate</RequestOption>" +
                "</Request>" +
                "<Shipment>" +
                "<Shipper>" +
                "<Name>Ashlin BPG Marketing Inc</Name>" +
                "<PhoneNumber>9058553027</PhoneNumber>" +
                "<ShipperNumber>" + ACCOUNT_NUMBER + "</ShipperNumber>" +
                "<Address>" +
                "<AddressLine1>2351 Royal Windsor Dr</AddressLine1>" +
                "<City>Mississauga</City>" +
                "<StateProvinceCode>ON</StateProvinceCode>" +
                "<PostalCode>L5J4S7</PostalCode>" +
                "<CountryCode>CA</CountryCode>" +
                "</Address>" +
                "</Shipper>" +
                "<ShipTo>" +
                "<CompanyName>Sears</CompanyName>" +
                "<PhoneNumber>" + value.Recipient.DayPhone + "</PhoneNumber>" +
                "<Address>" +
                "<AddressLine1>" + value.ShipTo.Address1 + "</AddressLine1>";
            if (value.ShipTo.Address2 != "")
                textXML += "<AddressLine2>" + value.ShipTo.Address2 + "</AddressLine2>";
            textXML +=
            "<City>" + value.ShipTo.City + "</City>" +
            "<StateProvinceCode>" + value.ShipTo.State + "</StateProvinceCode>" +
            "<PostalCode>" + value.ShipTo.PostalCode + "</PostalCode>" +
            "<CountryCode>CA</CountryCode>" +
            "</Address>" +
            "</ShipTo>" +
            "<PaymentInformation>" +
            "<BillThirdParty>" +
            "<BillThirdPartyShipper>" +
            "<AccountNumber>" + SEARS_ACCOUNT_NUMBER + "</AccountNumber>" +
            "<ThirdParty>" +
            "<Address>" +
            "<PostalCode>L5J4S7</PostalCode>" +
            "<CountryCode>CA</CountryCode>" +
            "</Address>" +
            "</ThirdParty>" +
            "</BillThirdPartyShipper>" +
            "</BillThirdParty>" +
            "</PaymentInformation>" +
            "<Service>";

            string code;
            switch (package.Service)
            {
                case "UPS Standard":
                    code = "11";
                    break;
                case "UPS 3 Day Select":
                    code = "12";
                    break;
                case "UPS Worldwide Express":
                    code = "07";
                    break;
                default:
                    code = "01";
                    break;
            }

            textXML +=
            "<Code>" + code + "</Code>" +
            "</Service>" +
            "<Package>";

            switch (package.PackageType)
            {
                case "Letter":
                    code = "01";
                    break;
                case "Express Box":
                    code = "21";
                    break;
                case "First Class":
                    code = "59";
                    break;
                default:
                    code = "02";
                    break;
            }

            textXML +=
            "<PackagingType>" +
            "<Code>" + code + "</Code>" +
            "</PackagingType>" +
            "<Dimensions>" +
            "<UnitOfMeasurement>" +
            "<Code>CM</Code>" +
            "</UnitOfMeasurement>" +
            "<Length>" + package.Length + "</Length>" +
            "<Width>" + package.Width + "</Width>" +
            "<Height>" + package.Height + "</Height>" +
            "</Dimensions>" +
            "<PackageWeight>" +
            "<UnitOfMeasurement>" +
            "<Code>KGS</Code>" +
            "</UnitOfMeasurement>" +
            "<Weight>" + package.Weight + "</Weight>" +
            "</PackageWeight>" +
            "</Package>" +
            "</Shipment>" +
            "<LabelSpecification>" +
            "<LabelPrintMethod>" +
            "<Code>GIF</Code>" +
            "</LabelPrintMethod>" +
            "<LabelImageFormat>" +
            "<Code>GIF</Code>" +
            "</LabelImageFormat>" +
            "</LabelSpecification>" +
            "</ShipmentConfirmRequest>";

            // turn request string into a byte stream
            byte[] postBytes = Encoding.UTF8.GetBytes(textXML);

            // send request
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postBytes, 0, postBytes.Length);
            }

            // get the response from the server
            HttpWebResponse response;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch
            {
                // the case if server error
                return null;
            }

            string result;
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            // get the response stattus
            result = substringMethod(result, "ResponseStatusCode", 19);
            string responseStatus = getTarget(result);

            // get identification number and shipment digest
            string[] returnString = new string[2];
            if (responseStatus == "1")
            {
                result = substringMethod(result, "ShipmentIdentificationNumber", 29);
                returnString[0] = getTarget(result);

                result = substringMethod(result, "ShipmentDigest", 15);
                returnString[1] = getTarget(result);
            }
            else
            {
                // the case if bad request
                string[] error = { "Error: " + getTarget(substringMethod(result, "ErrorDescription", 17)) };
                return error;
            }

            return returnString;
        }

        /* a method that post shipment accept request and return base64 image string and tracking number*/
        public string[] postShipmentAccept(string shipmentDigest)
        {
            string shipmentAcceptmUri = "https://wwwcie.ups.com/ups.app/xml/ShipAccept";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(shipmentAcceptmUri);
            request.Method = "POST";
            request.ContentType = "application/xml";

            string textXML =
               "<?xml version=\"1.0\"?>" +
               "<AccessRequest xml:lang=\"en-US\">" +
               "<AccessLicenseNumber>" + ACCESS_LISCENSE_NUMBER + "</AccessLicenseNumber>" +
               "<UserId>" + USER_ID + "</UserId>" +
               "<Password>" + PASSWORD + "</Password>" +
               "</AccessRequest>" +
               "<?xml version=\"1.0\"?>" +
               "<ShipmentAcceptRequest>" +
               "<Request>" +
               "<RequestAction>ShipAccept</RequestAction>" +
               "</Request>" +
               "<ShipmentDigest>" + shipmentDigest +
               "</ShipmentDigest></ShipmentAcceptRequest>";

            // turn request string into a byte stream
            byte[] postBytes = Encoding.UTF8.GetBytes(textXML);

            // send request
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postBytes, 0, postBytes.Length);
            }

            // get the response from the server
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string result;
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            // get the response stattus
            result = substringMethod(result, "ResponseStatusCode", 19);
            string responseStatus = getTarget(result);

            // get tracking number and image
            string[] text = new string[2];
            if (responseStatus == "1")
            {
                result = substringMethod(result, "TrackingNumber", 15);
                text[0] = getTarget(result);

                result = substringMethod(result, "GraphicImage", 13);
                text[1] = getTarget(result);
            }
            else
            {
                // the case if bad request
                return null;
            }

            return text;
        }

        /* a method that post shipment void request and check if the void request is success */
        public string postShipmentVoid(string identificationNumber)
        {
            string shipmentAcceptmUri = "https://wwwcie.ups.com/ups.app/xml/Void";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(shipmentAcceptmUri);
            request.Method = "POST";
            request.ContentType = "application/xml";

            string textXML =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<AccessRequest xml:lang=\"en-US\">" +
                "<AccessLicenseNumber>" + ACCESS_LISCENSE_NUMBER + "</AccessLicenseNumber>" +
                "<UserId>" + USER_ID + "</UserId>" +
                "<Password>" + PASSWORD + "</Password>" +
                "</AccessRequest>" +
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<VoidShipmentRequest>" +
                "<Request>" +
                "<RequestAction>1</RequestAction>" +
                "</Request>" +
                "<ShipmentIdentificationNumber>" + "1ZISDE016691676846" + "</ShipmentIdentificationNumber>" +
                "</VoidShipmentRequest>";

            // turn request string into a byte stream
            byte[] postBytes = Encoding.UTF8.GetBytes(textXML);

            // send request
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postBytes, 0, postBytes.Length);
            }

            // get the response from the server
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string result;
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            // get the response stattus
            result = substringMethod(result, "ResponseStatusCode", 19);
            string responseStatus = getTarget(result);

            // the case is bad request
            if (responseStatus != "1")
                return "Error: " + getTarget(substringMethod(result, "ErrorDescription", 17));

            return null;
        }
        #endregion

        /* a method that turn base64 string into GIF format image */
        public void exportLabel(string base64String, string transactionId, bool preview)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);

            // save image
            // check if the save directory exist -> if not create it
            if (!File.Exists(savePathSears))
                Directory.CreateDirectory(savePathSears);

            // save the image
            string file = savePathSears + "\\" + transactionId + ".gif";
            image.Save(file, System.Drawing.Imaging.ImageFormat.Gif);

            // show the image if user want to preview
            if (preview)
            {
                if (System.Diagnostics.Process.GetProcessesByName(file).Length < 1)
                    System.Diagnostics.Process.Start(file);
            }
        }

        /*  a Get for savepath for shipment label */
        public string SavePathSears
        {
            get
            {
                return savePathSears;
            }
        }

        #region Supporting Methods
        /* a method that substring the given string */
        private string substringMethod(string original, string startingString, int additionIndex)
        {
            return original.Substring(original.IndexOf(startingString) + additionIndex);
        }

        /* a method that get the next target token */
        private string getTarget(string text)
        {
            int i = 0;
            while (text[i] != '<' && text[i] != '>' && text[i] != '"')
                i++;

            return text.Substring(0, i);
        }
        #endregion
    }
}
