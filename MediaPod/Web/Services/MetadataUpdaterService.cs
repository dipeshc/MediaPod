using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using MediaPod.Extractors;
using MediaPod.Interfaces;
using MediaPod.Interfaces.Models;
using MediaPod.Managers;
using MediaPod.Model.Extensions;
using MediaPod.Tasks;
using MediaPod.Web.Routes;
using MediaPod.Web.Extensions;
using ServiceStack.Text;

namespace MediaPod.Web.Services
{
	public class MetadataUpdaterService : BaseService
	{
		public object Get(MetadataUpdaterTVShowRequest request)
		{
			return RenderViewOptimized ("MetadataUpdater.TVShow", new { File = request.Path.FromSiteFilePath() });
		}

		public object Get(MetadataUpdaterTVShowSearchRequest request)
		{
			var results = ResourceManager.MetadataSource.Search(request.TVShowName, request.SeasonNumber, request.EpisodeNumber.Value);
			return RenderViewOptimized ("MetadataUpdater.TVShowSearchResults", new { Results = results });
		}

		public object Post(MetadataUpdaterTVShowUpdateRequest request)
		{
			// Convert the request to a tvshow.
			var tvShow = new MetadataUpdaterTVShow(request);

			// Create notifications.
			var notificationQueued = new Notification()
			{
				Id = "",
				Heading = "Metadata Updater Request Queued",
				Message = string.Format("Queued metadata update on file {0}", request.Path.UrlDecode()),
				Type = NotificationType.Informational
			};
			var notificationStarted = new Notification()
			{
				Id = "",
				Heading = "Metadata Updater Request Started",
				Message = string.Format("Started metadata update on file {0}", request.Path.UrlDecode()),
				Type = NotificationType.Informational
			};
			var notificationCompleted = new AutoExpireNotification(new TimeSpan(0, 0, 30))
			{
				Id = "",
				Heading = "Metadata Updater Request Completed",
				Message = string.Format("Completed metadata update on file {0}", request.Path.UrlDecode()),
				Type = NotificationType.Success
			};
			var notificationError = new AutoExpireNotification(new TimeSpan(0, 2, 0))
			{
				Id = "",
				Heading = "Metadata Updater Request Errored",
				Message = string.Format("Error occured during metadata update on file {0}", request.Path.UrlDecode()),
				Type = NotificationType.Error
			};

			// Create task.
			var task = new RemuxEncodeMetadataAndAddToLibrary(new FileSystem(), tvShow, false);
			task.PreInvokeHandle = () =>
			{
				notificationQueued.HasExpired = true;
				ResourceManager.NotificationManager.NewNotification(notificationStarted);
			};
			task.PostInvokeHandle = () =>
			{
				notificationQueued.HasExpired = true;
				notificationStarted.HasExpired = true;
				notificationError.HasExpired = true;
				ResourceManager.NotificationManager.NewNotification(notificationCompleted);
			};
			task.InvokeErrorHandle = () =>
			{
				notificationQueued.HasExpired = true;
				notificationStarted.HasExpired = true;
				notificationCompleted.HasExpired = true;
				ResourceManager.NotificationManager.NewNotification(notificationError);
			};
			ResourceManager.QueuedTaskManager.Enqueue(task);
			ResourceManager.NotificationManager.NewNotification(notificationQueued);

			// Return null.
			return null;
		}

		private class MetadataUpdaterTVShow : ITVShow
		{
			public FileInfoBase File { get; private set; }
			public Definition Definition { get; private set; }
			public string TVShowName { get; private set; }
			public int? SeasonNumber { get; private set; }
			public int EpisodeNumber { get; private set; }
			public string Name { get; private set; }
			public string Description { get; private set; }
			public IEnumerable<string> Genres { get; private set; }
			public IEnumerable<string> Cast { get; private set; }
			public IEnumerable<string> Directors { get; private set; }
			public IEnumerable<string> Screenwriters { get; private set; }
			public DateTime ReleaseDate { get; private set; }
			public string Network { get; private set; }
			public IBase64Image Artwork { get; private set; }

			public MetadataUpdaterTVShow(MetadataUpdaterTVShowUpdateRequest request)
			{
				File = request.Path.FromSiteFilePath();
				Definition = MediaPod.Interfaces.Models.Definition.Unknown;
				TVShowName = request.TVShowName;
				SeasonNumber = Convert.ToInt32(request.SeasonNumber);
				EpisodeNumber = Convert.ToInt32(request.EpisodeNumber);
				Name = request.Name;
				Description = request.Description;
				Genres = request.Genres.Split(',').Select(genre => genre.Trim()).ToList();
				Cast = request.Cast.Split(',').Select(cast => cast.Trim()).ToList();
				Directors = request.Directors.Split(',').Select(director => director.Trim()).ToList();
				Screenwriters = request.Screenwriters.Split(',').Select(screenwriters => screenwriters.Trim()).ToList();
				ReleaseDate = DateTime.ParseExact(request.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
				Network = request.Network;
				Artwork = request.Artwork.FromWebImage();

				// Determine definition.
				var streams = new MediaStreamsExtractor(File).Extract();
				var height = streams.Where(stream => stream is MediaStreamsExtractor.IVideoStream).Select(stream => ((MediaStreamsExtractor.IVideoStream) stream).Height).OrderByDescending(h => h).First();
				if(height >= 1080)
				{
					Definition = Definition.HD1080;
				}
				else if(height >= 720)
				{
					Definition = Definition.HD720;
				}
				else
				{
					Definition = Definition.SD;
				}
			}
		}

		private class Notification : INotification
		{
			public string Id { get; set; }
			public string Heading { get; set; }
			public string Message { get; set; }
			public NotificationType Type { get; set; }
			public bool IsSeen { get; set; }
			public bool HasExpired { get; set; }

			public Notification()
			{
				IsSeen = false;
				HasExpired = false;
			}

			public virtual void Seen()
			{
				IsSeen = true;
			}
		}

		private class AutoExpireNotification : Notification
		{
			private TimeSpan _autoExpireDurationUponSeen;

			public AutoExpireNotification(TimeSpan autoExpireDurationUponSeen) : base()
			{
				_autoExpireDurationUponSeen = autoExpireDurationUponSeen;
			}

			public override void Seen()
			{
				if (IsSeen)
				{
					return;
				}

				IsSeen = true;
				var thread = new Thread(() =>
				{
					Thread.Sleep(_autoExpireDurationUponSeen);
					HasExpired = true;
				});
				thread.Priority = ThreadPriority.Lowest;
				thread.Start();
			}
		}
	}
}