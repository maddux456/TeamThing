#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Common;
using System.Collections.Generic;
using Telerik.OpenAccess;
using Telerik.OpenAccess.Metadata;
using Telerik.OpenAccess.Data.Common;
using TeamThing.Model;


namespace TeamThing.Model	
{
	public partial class ThingBase : IThing
	{
		private string _taskType;
		public virtual string Task_type 
		{ 
		    get
		    {
		        return this._taskType;
		    }
		    set
		    {
		        this._taskType = value;
		    }
		}
		
		private int _thingId;
		public virtual int ThingId 
		{ 
		    get
		    {
		        return this._thingId;
		    }
		    set
		    {
		        this._thingId = value;
		    }
		}
		
		private string _description;
		public virtual string Description 
		{ 
		    get
		    {
		        return this._description;
		    }
		    set
		    {
		        this._description = value;
		    }
		}
		
		private TeamMember _teamMember;
		public virtual TeamMember TeamMember 
		{ 
		    get
		    {
		        return this._teamMember;
		    }
		    set
		    {
		        this._teamMember = value;
		    }
		}
		
		private DateTime _dateCreated;
		public virtual DateTime DateCreated 
		{ 
		    get
		    {
		        return this._dateCreated;
		    }
		    set
		    {
		        this._dateCreated = value;
		    }
		}
		
		private ThingSource _thingSource;
		public virtual ThingSource ThingSource 
		{ 
		    get
		    {
		        return this._thingSource;
		    }
		    set
		    {
		        this._thingSource = value;
		    }
		}
		
	}
}
