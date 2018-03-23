using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Corp.Integration.Utility.NotificationSvc
{
    public class NotificationService : INotificationService
    {
        public void InitializeNotification(string processName, string ServiceId, string IssueCategory)
        {
            Trace.WriteLine("Corp.Integration.Utility.NotificationSvc service initializing. ");

            try
            {
                //GetNotificationConfiguration()
                DBConnection dbConn = new DBConnection();
                List<NotificationLog> notificationsLogsUnProcessed;
                List<NotificationLog> notificationsLogsProcessed;

                //Get notification configurations
                List<string> notificationConfiguration = dbConn.GetNotificationConfiguration("NriShippingConfirmation", "NriShippingSvc", "Mapping");

                if (notificationConfiguration != null && notificationConfiguration.Any())
                {
                    //Get unprocessed NotificationLogs
                    notificationsLogsUnProcessed = dbConn.GetNotificationLogs(0, "NriShippingConfirmation", "NriShippingSvc", "Mapping");

                    //Get processed NotificationLogs
                    notificationsLogsProcessed = dbConn.GetNotificationLogs(1, "NriShippingConfirmation", "NriShippingSvc", "Mapping", Convert.ToInt32(notificationConfiguration[6]));

                    if ( notificationsLogsProcessed != null && notificationsLogsProcessed.Any())
                    {
                        //exclude already processed notification logs if they are present in unprocessed notification logs
                        var alreadyProcessedNotifications = from unProcessed in notificationsLogsUnProcessed
                                                            from processed in notificationsLogsProcessed
                                                            where ((unProcessed.IssueMessage == processed.IssueMessage) && (unProcessed.DocumentId == processed.DocumentId))
                                                            select unProcessed;

                        var notificationsLogs = notificationsLogsUnProcessed.Except(alreadyProcessedNotifications);

                        //remove duplicate unprocessed and processed notification logs
                        var notificationLog = notificationsLogs.GroupBy(x => new { x.IssueMessage, x.DocumentId }).Select(y => y.FirstOrDefault());

                        var listIssueMessage = notificationLog.Select(o => o.IssueMessage);

                        var listNotificationLogIds = notificationsLogsUnProcessed.Select(o => o.NotificationLogId);

                        if (notificationLog != null && notificationLog.Any())
                        {
                            Email email = new Email();
                            string emailbody = email.SendEmail(notificationConfiguration[0], notificationConfiguration[1], notificationConfiguration[2], listIssueMessage.ToList(), notificationConfiguration[3]);

                            int notificationId = dbConn.InsertNotification(Convert.ToInt32(notificationConfiguration[7]), notificationConfiguration[2], emailbody, notificationConfiguration[0], notificationConfiguration[1]);

                            //Set IsProcessed flag to true
                            dbConn.UpdateNotificationLog(1, notificationId, listNotificationLogIds.ToList());
                        }
                        else if (notificationConfiguration[4] == "1")
                        {
                            Email email = new Email();
                            email.SendEmail(notificationConfiguration[0], notificationConfiguration[1], notificationConfiguration[2], notificationConfiguration[5]);
                        }
                    }
                    else if(notificationsLogsUnProcessed != null && notificationsLogsUnProcessed.Any())
                    {
                        //remove duplicate unprocessed notification logs
                        var notificationLogs = notificationsLogsUnProcessed.GroupBy(x => new { x.IssueMessage, x.DocumentId }).Select(y => y.FirstOrDefault());

                        var listIssueMessage = notificationLogs.Select(o => o.IssueMessage);

                        var listNotificationLogIds = notificationsLogsUnProcessed.Select(o => o.NotificationLogId);

                        if (notificationLogs != null && notificationLogs.Any())
                        {
                            Email email = new Email();
                            string emailbody = email.SendEmail(notificationConfiguration[0], notificationConfiguration[1], notificationConfiguration[2], listIssueMessage.ToList(), notificationConfiguration[3]);

                            int notificationId = dbConn.InsertNotification(Convert.ToInt32(notificationConfiguration[7]), notificationConfiguration[2], emailbody, notificationConfiguration[0], notificationConfiguration[1]);

                            //Set IsProcessed flag to true
                            dbConn.UpdateNotificationLog(1, notificationId, listNotificationLogIds.ToList());
                        }
                        else if (notificationConfiguration[4] == "1")
                        {
                            Email email = new Email();
                            email.SendEmail(notificationConfiguration[0], notificationConfiguration[1], notificationConfiguration[2], notificationConfiguration[5]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Corp.Integration.Utility.NotificationSvc service exception occurred. Exception message: " + ex.Message);
            }
        }
    }

}
