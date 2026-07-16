using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FitISO.Maui.Models
{
    public partial class Set : ObservableObject
    {
        [ObservableProperty]
        int id;
        [ObservableProperty]
        double weight;
        [ObservableProperty]
        double reps;

        public Set()
        {

        }

        public Set(FitISO.Data.Models.Set set)
        {
            Id = set.Id;
            Weight = set.Weight;
            Reps = set.Reps;
        }

    }
}
