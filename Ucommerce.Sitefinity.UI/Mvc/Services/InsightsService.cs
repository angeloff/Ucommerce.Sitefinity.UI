﻿using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Telerik.DigitalExperienceCloud.Client;
using Telerik.Sitefinity.DataIntelligenceConnector.Configuration;
using Telerik.Sitefinity.DataIntelligenceConnector.Managers;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.TrackingConsent;
using Telerik.Sitefinity.Web;
using Ucommerce.Api;
using Ucommerce.Extensions;
using Ucommerce.Infrastructure;
using Ucommerce.Search.Facets;
using UCommerce.Sitefinity.UI.Search;

namespace UCommerce.Sitefinity.UI.Mvc.Services
{
	public interface IInsightsService
	{
		void SendInteraction(Ucommerce.EntitiesV2.Category category, string predicate, string objectName);
		void SendInteraction(Ucommerce.Search.Models.Category category, string predicate, string objectName);
		void SendInteraction(Ucommerce.EntitiesV2.Product product, string predicate, string objectName);
		void SendInteraction(Ucommerce.Search.Models.Product product, string predicate, string objectName);
		void SendInteraction(Ucommerce.EntitiesV2.Customer customer, string predicate, string objectName);
		void SendBasketInteraction(string predicate, string objectName);
		void SendInteraction(Ucommerce.EntitiesV2.PurchaseOrder order, string predicate, string objectName);
	}

	public class InsightsService : IInsightsService
	{
		private readonly ITransactionLibrary _transactionLibrary = ObjectFactory.Instance.Resolve<ITransactionLibrary>();

		private IInteractionManager _interactionManager;
		public IInteractionManager InteractionManager
		{
			get
			{
				if (_interactionManager != null) return _interactionManager;

				var moduleEnabled = Telerik.Sitefinity.Abstractions.ObjectFactory.IsTypeRegistered<IInteractionManager>();
				if (!moduleEnabled) return null;

				var canTrack = TrackingConsentManager.CanTrackCurrentUser();
				if (!canTrack)
					return null;

				var insightConfigWrapper = Telerik.Sitefinity.Abstractions.ObjectFactory.Resolve<IDataIntelligenceConnectorConfigWrapper>();
				if (insightConfigWrapper?.HasValidConnectionForCurrentSite ?? false)
					_interactionManager = Telerik.Sitefinity.Abstractions.ObjectFactory.Resolve<IInteractionManager>();

				return _interactionManager;
			}
		}

		public void SendInteraction(Ucommerce.EntitiesV2.Category category, string predicate, string objectName)
		{
			var interaction = CreateInteractionForBasket(predicate, objectName);
			if (interaction == null) return;

			AddBasketInteractions(interaction);
			AddCategoryInteractions(interaction, category);
			ImportInteraction(interaction);
		}

		public void SendInteraction(Ucommerce.Search.Models.Category category, string predicate, string objectName)
		{
			var interaction = CreateInteractionForBasket(predicate, objectName);
			if (interaction == null) return;

			AddBasketInteractions(interaction);
			AddCategoryInteractions(interaction, category);
			ImportInteraction(interaction);
		}

		public void SendInteraction(Ucommerce.EntitiesV2.Product product, string predicate, string objectName)
		{
			var interaction = CreateInteractionForBasket(predicate, objectName);
			if (interaction == null) return;

			AddBasketInteractions(interaction);
			AddProductInteractions(interaction, product);
			ImportInteraction(interaction);
		}

		public void SendInteraction(Ucommerce.Search.Models.Product product, string predicate, string objectName)
		{
			var interaction = CreateInteractionForBasket(predicate, objectName);
			if (interaction == null) return;

			AddBasketInteractions(interaction);
			AddProductInteractions(interaction, product);
			ImportInteraction(interaction);
		}

		public void SendInteraction(Ucommerce.EntitiesV2.Customer customer, string predicate, string objectName)
		{
			var interaction = CreateInteractionForBasket(predicate, objectName);
			if (interaction == null) return;

			AddBasketInteractions(interaction);
			AddCustomerInteractions(interaction, customer);
			ImportInteraction(interaction);
		}

		public void SendBasketInteraction(string predicate, string objectName)
		{
			SendInteraction(SafeGetOrder(), predicate, objectName);
		}

		public void SendInteraction(Ucommerce.EntitiesV2.PurchaseOrder order, string predicate, string objectName)
		{
			var interaction = GetInteractionForOrder(predicate, objectName, order);
			if (interaction == null) return;

			AddOrderInteractions(interaction, order);
			ImportInteraction(interaction);
		}

		private Interaction CreateInteractionForBasket(string predicate, string objectName)
		{
			var order = SafeGetOrder();
			return GetInteractionForOrder(predicate, objectName, order);
		}

		private Ucommerce.EntitiesV2.PurchaseOrder SafeGetOrder()
		{
			Ucommerce.EntitiesV2.PurchaseOrder order = null;
			try
			{
				order = _transactionLibrary.GetBasket();
			}
			catch (Exception ex)
			{
				// TODO: Log
			}

			return order;
		}

		private Interaction GetInteractionForOrder(string predicate, string objectName, Ucommerce.EntitiesV2.PurchaseOrder order)
		{
			if (InteractionManager == null) return null;

			var subject = InteractionManager.GetTrackingId(); // order?.OrderGuid.ToString() ?? FakeBasketId();
			var interaction = new Interaction()
			{
				Subject = subject, // TODO: Change this to the tracking cookie from the API IInteractionManager
								   //Subject = _interactionManager.GetTrackingId(),
				Predicate = predicate,
				Object = objectName
			};
			return interaction;
		}

		private void ImportInteraction(Interaction interaction)
		{
			if (interaction == null) return;

			_interactionManager.SendInteraction(interaction);
		}

		private void AddBasketInteractions(Interaction interaction)
		{
			if (interaction == null) return;

			var order = SafeGetOrder();
			AddOrderInteractions(interaction, order);
		}

		private void AddCategoryInteractions(Interaction interaction, Ucommerce.EntitiesV2.Category category)
		{
			if (interaction == null) return;

			if (category == null) return;

			const string prefix = "Category";
			AddBaseSiteInfo(interaction, category.Guid, "Product List");
			AddObjectHierarchyData(interaction, "CategoryId", category.CategoryId);
			AddObjectHierarchyData(interaction, "CategoryName", category.Name);
			AddObjectHierarchyData(interaction, "CategoryDisplayName", category.DisplayName());

			interaction.ObjectMetadata.Title = category.DisplayName();
			interaction.ObjectMetadata.CanonicalTitle = category.Name;

			foreach (var property in category.CategoryProperties)
				AddObjectMetaData(interaction, prefix, property.DefinitionField.Name, property.Value);

			AddFacets(interaction);
		}

		private void AddCategoryInteractions(Interaction interaction, Ucommerce.Search.Models.Category category)
		{
			if (interaction == null) return;

			if (category == null) return;

			const string prefix = "Category";
			AddBaseSiteInfo(interaction, category.Guid, "Product List");
			AddObjectHierarchyData(interaction, "CategoryName", category.Name);
			AddObjectHierarchyData(interaction, "CategoryDisplayName", category.DisplayName);

			interaction.ObjectMetadata.Title = category.DisplayName;
			interaction.ObjectMetadata.CanonicalTitle = category.Name;

			foreach (var property in category.GetUserDefinedFields())
				AddObjectMetaData(interaction, prefix, property.Key, property.Value);

			AddFacets(interaction);
		}

		private void AddProductInteractions(Interaction interaction, Ucommerce.EntitiesV2.Product product)
		{
			if (interaction == null) return;

			if (product == null) return;

			const string prefix = "Product";
			AddBaseSiteInfo(interaction, product.Guid, "Product");
			AddObjectHierarchyData(interaction, "Id", product.ProductId);
			AddObjectHierarchyData(interaction, "ProductDefinition", product.ProductDefinition.Name);
			AddObjectHierarchyData(interaction, "Sku", product.Sku);
			AddObjectHierarchyData(interaction, "VariantSku", product.VariantSku);
			AddObjectHierarchyData(interaction, "ProductName", product.Name);
			AddObjectHierarchyData(interaction, "ProductDisplayName", product.DisplayName());

			interaction.ObjectMetadata.Title = product.DisplayName();
			interaction.ObjectMetadata.CanonicalTitle = product.Name;

			foreach (var property in product.ProductProperties)
				AddObjectHierarchyData(interaction, property.ProductDefinitionField.Name, property.Value);

			AddFacets(interaction);
		}

		private void AddProductInteractions(Interaction interaction, Ucommerce.Search.Models.Product product)
		{
			if (interaction == null) return;

			if (product == null) return;

			const string prefix = "Product";
			AddBaseSiteInfo(interaction, product.Guid, "Product");
			AddObjectHierarchyData(interaction, "ProductGuid", product.Guid);
			// TODO: AddObjectHierarchyData(interaction, prefix, "ProductDefinition", product.ProductDefinition.Name);
			AddObjectHierarchyData(interaction, "Sku", product.Sku);
			AddObjectHierarchyData(interaction, "VariantSku", product.VariantSku);
			AddObjectHierarchyData(interaction, "ProductName", product.Name);
			AddObjectHierarchyData(interaction, "ProductDisplayName", product.DisplayName);

			interaction.ObjectMetadata.Title = product.DisplayName;
			interaction.ObjectMetadata.CanonicalTitle = product.Name;

			foreach (var property in product.GetUserDefinedFields())
				AddObjectHierarchyData(interaction, property.Key, property.Value);

			AddFacets(interaction);
		}

		private static void AddBaseSiteInfo(Interaction interaction, Guid guid, string contentType)
		{
			interaction.ObjectMetadata.Id = guid.ToString();
			interaction.ObjectMetadata.ContentType = contentType;

			var currentSite = SystemManager.CurrentContext.CurrentSite;
			interaction.ObjectMetadata.SiteName = currentSite.Name;

			interaction.ObjectMetadata.Add("SFDataProviderName", "Ucommerce");

			if (SystemManager.CurrentContext.CurrentSite.Cultures.Length > 1)
			{
				// provide language if site is multilingual 
				interaction.ObjectMetadata.Language = SystemManager.CurrentContext.Culture.Name;
			}

			var pageUrl = SystemManager.CurrentHttpContext?.Request?.Url?.AbsoluteUri ?? string.Empty;

			if (string.IsNullOrWhiteSpace(pageUrl)) return;

			var pagePath = new Uri(pageUrl).AbsolutePath;
			pagePath = HttpUtility.UrlDecode(pagePath);

			interaction.ObjectMetadata.ReferrerUrl = pageUrl;

			var pageSiteNode = FindSiteMapNode(pagePath);
			if (pageSiteNode == null) return;

			var pageId = pageSiteNode.Id.ToString();
			interaction.ObjectMetadata.PageId = pageId;
			interaction.ObjectMetadata.ReferrerId = pageId;
		}

		private static PageSiteNode FindSiteMapNode(string pageUrl)
		{
			var siteMapProvider = SiteMapBase.GetCurrentProvider();
			var pageSiteNode = siteMapProvider.FindSiteMapNode(pageUrl) as PageSiteNode;
			return pageSiteNode;
		}

		private static void AddOrderInteractions(Interaction interaction, Ucommerce.EntitiesV2.PurchaseOrder order)
		{
			if (interaction == null) return;

			if (order == null) return;

			const string prefix = "Order";
			AddObjectHierarchyData(interaction, "CultureCode", order.CultureCode);
			AddObjectHierarchyData(interaction, "BasketId", order.BasketId.ToString());
			AddObjectHierarchyData(interaction, "OrderGuid", order.OrderGuid.ToString());
			AddObjectHierarchyData(interaction, "BillingCurrency", order.BillingCurrency.ISOCode);

			foreach (var property in order.OrderProperties)
				AddObjectHierarchyData(interaction, $"Property_{property.Key}", property.Value);

			AddAddressInteractions(interaction, order.BillingAddress);
			AddCustomerInteractions(interaction, order.Customer);
		}

		private static void AddCustomerInteractions(Interaction interaction, Ucommerce.EntitiesV2.Customer customer)
		{
			if (interaction == null) return;

			if (customer == null) return;

			interaction.SubjectMetadata.Add("Email", customer.EmailAddress);
		}

		private static void AddAddressInteractions(Interaction interaction, Ucommerce.EntitiesV2.IAddress address)
		{
			if (interaction == null) return;

			if (address == null) return;

			interaction.SubjectMetadata.Add("FirstName", address.FirstName);
			interaction.SubjectMetadata.Add("LastName", address.LastName);
			interaction.SubjectMetadata.Add("Email", address.EmailAddress);
			interaction.SubjectMetadata.Add("Country", address.Country.Name);
			interaction.SubjectMetadata.Add("CountryISO", address.Country.TwoLetterISORegionName);
		}

		private void AddFacets(Interaction interaction)
		{
			if (interaction == null) return;

			var facets = HttpContext.Current.Request.QueryString.ToFacets();
			if (!(facets?.Any() ?? false)) return;

			const string prefix = "Facet";
			foreach (var facet in facets)
				AddFacetValues(interaction, facet);
		}

		private static void AddFacetValues(Interaction interaction, Facet facet)
		{
			if (interaction == null) return;

			// NOTE: We can't currently send through multiple values with the same id
			//foreach (var value in facet.FacetValues)
			//	AddSubjectMetaData(interaction, prefix, facet.Name, value.Value);
			// AddObjectMetaData(interaction, prefix, facet.Name, string.Join("|", facet.FacetValues.Select(f => f.Value)));
			foreach (var value in facet.FacetValues)
				AddObjectHierarchyData(interaction, facet.Name.ToLower(), value.Value.ToLower());
		}

		private static void AddObjectMetaData(Interaction interaction, string prefix, string key, object valueToAdd)
		{
			if (interaction == null) return;

			if (valueToAdd == null) return;

			var value = valueToAdd.ToString();

			if (string.IsNullOrWhiteSpace(value)) return;

			var dataKey = $"{prefix}{key}";
			if (interaction.ObjectMetadata.ContainsKey(dataKey))
				interaction.ObjectMetadata.Add(dataKey, value);
			else
				interaction.ObjectMetadata[dataKey] = value;
		}

		private static void AddObjectHierarchyData(Interaction interaction, string key, object valueToAdd)
		{
			if (interaction == null) return;

			if (valueToAdd == null) return;

			var value = valueToAdd.ToString();

			if (string.IsNullOrWhiteSpace(value)) return;

			var valueGuid = CalculateHash($"{key}_{value}");
			var parentGuid = CalculateHash(key);

			var hierarchy = new HierarchicalMetadata
				{
					{ "Id", valueGuid },
					{ "Title", $"{value}" }
				};

			hierarchy.Parent = new HierarchicalMetadata()
					{
						{ "Id", parentGuid },
						{ "Title", $"{key}" }
					};
			interaction.ObjectMetadata.Hierarchies.Add(hierarchy);

		}

		private static string CalculateHash(string valueToHash)
		{
			using (var md5 = MD5.Create())
			{
				var hash = md5.ComputeHash(Encoding.Default.GetBytes(valueToHash.ToLower().Trim()));
				var guid = new Guid(hash);
				return guid.ToString();
			}
		}
	}
}
