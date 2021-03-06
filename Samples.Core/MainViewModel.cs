﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Plugin.Geofencing;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using ReactiveUI;
using Acr.UserDialogs;
using Xamarin.Forms;


namespace Samples
{
    public class MainViewModel : ReactiveObject
    {
        readonly IGeolocator gps = CrossGeolocator.Current;
        readonly IGeofenceManager geofences = CrossGeofences.Current;


        public MainViewModel()
        {
            var current = this.geofences.MonitoredRegions.FirstOrDefault();
            if (current != null)
            {
                this.CenterLatitude = current.Center.Latitude;
                this.CenterLongitude = current.Center.Longitude;
                this.DistanceMeters = current.Radius.TotalMeters;
                this.HasGeofence = true;
            }

            this.StopGeofence = ReactiveCommand.Create(() =>
            {
                this.geofences.StopAllMonitoring();
                this.HasGeofence = false;
            },
            this.WhenAny(
                x => x.HasGeofence,
                x => x.Value
            ));

            this.SetGeofence = ReactiveCommand.Create(() =>
            {
                try
                {
                    this.geofences.StopAllMonitoring();

                    var radius = Distance.FromMeters(this.DistanceMeters.Value);
                    var center = new Plugin.Geofencing.Position(this.CenterLatitude.Value, this.CenterLongitude.Value);

                    this.geofences.StartMonitoring(new GeofenceRegion("plugintest", center, radius));
                    this.HasGeofence = true;

                    UserDialogs.Instance.Alert("Geofence set");
                }
                catch (Exception ex)
                {
                    UserDialogs.Instance.Alert(ex.ToString());
                }
            },
            this.WhenAny(
                x => x.CenterLatitude,
                x => x.CenterLongitude,
                x => x.DistanceMeters,
                x => x.HasGeofence,
                (latitude, longitude, dist, hasGeo) =>
                    latitude.Value > -90 &&
                    latitude.Value < 90 &&
                    longitude.Value > -180 &&
                    longitude.Value < 180 &&
                    dist.Value > 0 &&
                    dist.Value < 3000 &&
                    !hasGeo.Value
            ));
            //this.WhenAny(
            //        x => x.CenterLatitude,
            //        x => x.CenterLongitude,
            //        x => x.DistanceMeters,
            //        x => x.HasGeofence,
            //        (latitude, longitude, dist, hasGeo) =>
            //            latitude.Value > -180 &&
            //            latitude.Value < 180 &&
            //            longitude.Value > -90 &&
            //            longitude.Value < 90 &&
            //            dist.Value > 0 &&
            //            dist.Value < 3000 &&
            //            !hasGeo.Value
            //    )
            //    .Subscribe(x =>
            //    {
            //        var msg = $"LAT: {this.CenterLatitude} - LNG: {this.CenterLongitude} - D: {this.DistanceMeters} - HG: {this.HasGeofence}";
            //        UserDialogs.Instance.Alert(msg);
            //    });


            this.RequestStatus = ReactiveCommand.CreateFromTask(async ct =>
            {
                var region = this.geofences.MonitoredRegions.First();
                var result = await this.geofences.RequestState(region, ct).ConfigureAwait(false);
                UserDialogs.Instance.Alert("Geofence Status: " + result.ToString(), "Status");
            },
            this.WhenAny(
                x => x.HasGeofence,
                x => x.Value
            ));


            this.UseCurrentGps = ReactiveCommand.CreateFromTask(async ct =>
            {
                try
                {
                    var pos = await this.gps.GetPositionAsync(token: ct).ConfigureAwait(false);
                    if (pos != null)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            this.CenterLatitude = pos.Latitude;
                            this.CenterLongitude = pos.Longitude;
                        });
                    }
                }
                catch (Exception ex)
                {
                    UserDialogs.Instance.Alert("Error getting current location - " + ex);
                }
            });
        }


        public ICommand SetGeofence { get; }
        public ICommand UseCurrentGps { get; }
        public ICommand StopGeofence { get; }
        public ICommand RequestStatus { get; }


        bool hasGeofence;
        public bool HasGeofence
        {
            get => this.hasGeofence;
            set => this.RaiseAndSetIfChanged(ref this.hasGeofence, value);
        }


        double? lat;
        public double? CenterLatitude
        {
            get => this.lat;
            set => this.RaiseAndSetIfChanged(ref this.lat, value);
        }


        double? lng;
        public double? CenterLongitude
        {
            get => this.lng;
            set => this.RaiseAndSetIfChanged(ref this.lng, value);
        }


        double? meters = 200;
        public double? DistanceMeters
        {
            get => this.meters;
            set => this.RaiseAndSetIfChanged(ref this.meters, value);
        }
    }
}
