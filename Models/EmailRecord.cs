using System.ComponentModel.DataAnnotations;

namespace iMMMporter.Models
{
    public class EmailRecord
    {
        [Display(Name = "Content")]
        public string? Content { get; set; }
        
        [Display(Name = "Email")]
        public string? Email { get; set; }
        
        [Display(Name = "Recipients")]
        public string? Recipients { get; set; }
        
        [Display(Name = "Sender")]
        public string? Sender { get; set; }
        
        [Display(Name = "Sent Date")]
        [DataType(DataType.DateTime)]
        public DateTime? SentDate { get; set; }
        
        [Display(Name = "Subject")]
        public string? Subject { get; set; }
        
        // Maps the model properties to Dynamics 365 logical names
        public Dictionary<string, object> ToDynamicsEntity()
        {
            var entity = new Dictionary<string, object>();
            
            // Using the correct logical names for the fields
            if (Content != null) entity["mmm_content"] = Content;
            if (Email != null) entity["mmm_emailid"] = Email; // Updated to match logical name
            if (Recipients != null) entity["mmm_recipients"] = Recipients;
            if (Sender != null) entity["mmm_sender"] = Sender;
            if (SentDate != null) entity["mmm_sentdate"] = SentDate;
            if (Subject != null) entity["mmm_subject"] = Subject;
            
            return entity;
        }
    }
}