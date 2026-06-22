using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using TMPro;

namespace Unity.WebRequest.Email
{
    public class EmailSender : MonoBehaviour
    {
        [Header("SMTP")]
        [SerializeField] private string _smtpServer = "smtp.gmail.com";
        [SerializeField] private int _smtpPort = 587;

        [Header("Account")]
        [SerializeField] private string _senderEmail = "elevenvrdevs@gmail.com";
        [SerializeField] private string _appPassword = "TU_APP_PASSWORD";

        private string _destinationEmail;
        private string _imagePath;

        public void SaveLocalDestination(string path) => _imagePath = path;
        public void SaveEmailDestination(string email) => _destinationEmail = email;
        public void SaveEmailDestination(TMP_InputField field) => _destinationEmail = field.text;

        public async void SendEmailWithImage()
        {
            if (_destinationEmail == string.Empty || _imagePath == string.Empty) return;

            await Task.Run(() =>
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

                    MailMessage mail = new();
                    mail.From = new(_senderEmail);
                    mail.To.Add(_destinationEmail);

                    mail.Subject = "Foto Capturada";
                    mail.Body = "Adjunto encontrará la imagen capturada.";

                    Attachment attachment = new(_imagePath);
                    mail.Attachments.Add(attachment);

                    SmtpClient smtpClient = new SmtpClient(_smtpServer, _smtpPort);
                    smtpClient.Credentials = new NetworkCredential(_senderEmail, _appPassword);
                    smtpClient.EnableSsl = true;

                    smtpClient.Send(mail);
                    print("Correo enviado correctamente.");
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error enviando correo: " + ex.Message);
                }
            });
        }

        private static bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}