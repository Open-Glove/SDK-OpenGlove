using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Configuration;
using System.Configuration.Install;
using System.Threading;
using System.ServiceModel.Description;
using OpenGlove;
using InTheHand.Net.Sockets;
using System.Runtime.Serialization;
using System.Management;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;

namespace OpenGloveService
{
    public partial class OpenGloveService : ServiceBase
    {
        private ServiceHost m_svcHost = null;

        private const bool DEBUGGING = true;

        public OpenGloveService()
        {
            InitializeComponent();
            // Name the Windows Service
            ServiceName = "OpenGloveService";
        }

        // Start the Windows service.
        protected override void OnStart(string[] args)
        {
            if (DEBUGGING) Debugger.Launch();

            if (m_svcHost != null) m_svcHost.Close();

            m_svcHost = new ServiceHost(typeof(OpenGloveWCF.OGService));
            
            var endpoints = m_svcHost.Description.Endpoints;
            foreach (var endpoint in endpoints)
            {
                if (endpoint.Address.ToString().Equals("rest"))
                {
                    endpoint.EndpointBehaviors.Add(new OpenGloveWCF.CORSEnablingBehavior());
                }
            }
            
            m_svcHost.Open();
        }

        protected override void OnStop()
        {
            if (m_svcHost != null)
            {
                m_svcHost.Close();
                m_svcHost = null;
            }
        }
    }
}
