//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace importusers
{
    using System;
    using System.Collections.Generic;
    
    public partial class CustTable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CustTable()
        {
            this.EngChatTable = new HashSet<EngChatTable>();
        }
    
        public string CustId { get; set; }
        public string CompanyId { get; set; }
        public string TelephoneNumber { get; set; }
        public int RecId { get; set; }
        public Nullable<System.DateTime> CreateDateTime { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EngChatTable> EngChatTable { get; set; }
    }
}
