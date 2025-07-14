using MedicalImagingSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalImagingSystem.Messenge
{
    public class SwitchDicomImageViewModelMessage
    {
        public DicomImageViewModel Target { get; }
        public SwitchDicomImageViewModelMessage(DicomImageViewModel target)
        {
            Target = target;
        }
    }
}
