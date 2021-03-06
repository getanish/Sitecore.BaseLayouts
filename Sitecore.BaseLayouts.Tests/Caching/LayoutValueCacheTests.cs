﻿using System;
using Sitecore.BaseLayouts.Caching;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.FakeDb;
using Xunit;

namespace Sitecore.BaseLayouts.Tests.Caching
{
    public class LayoutValueCacheTests : FakeDbTestClass
    {
        [Fact]
        public void AddLayoutValue_WithFieldNotInCache_AddsEntry()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings()) {Enabled = true};
            cache.Clear();
            var field = MasterFakesFactory.CreateFakeLayoutField();

            // Act
            cache.AddLayoutValue(field.Item, field.Value);

            // Assert
            Assert.Equal(1, cache.InnerCache.Count);
        }

        [Fact]
        public void AddLayoutValue_WithFieldInCache_UpdatesEntry()
        {
            // Arrange
            var newValue = "This is the new layout value";
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings()) {Enabled = true};
            cache.Clear();
            var field = MasterFakesFactory.CreateFakeLayoutField();
            cache.AddLayoutValue(field.Item, field.Value);
            var initalCount = cache.InnerCache.Count;

            // Act
            cache.AddLayoutValue(field.Item, newValue);
            var result = cache.GetLayoutValue(field.Item);

            // Assert
            Assert.Equal(newValue, result);
            Assert.Equal(initalCount, cache.InnerCache.Count);
        }

        [Fact]
        public void AddLayoutValue_WithFieldsFromDifferentItems_AddsEntriesForBoth()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings()) {Enabled = true};
            cache.Clear();
            var field1 = MasterFakesFactory.CreateFakeLayoutField();
            var field2 = MasterFakesFactory.CreateFakeLayoutField();

            // Act
            cache.AddLayoutValue(field1.Item, field1.Value);
            cache.AddLayoutValue(field2.Item, field2.Value);

            // Assert
            Assert.Equal(2, cache.InnerCache.Count);
        }

        [Fact]
        public void AddLayoutValue_WithSameFieldInDifferentDatabases_AddsEntriesForBoth()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings()) {Enabled = true};
            cache.Clear();
            var id = new ID();
            var masterField = MasterFakesFactory.CreateFakeLayoutField(id);
            Field webField;
            using (var webDb = new Db("web"))
            {
                var webFakesFactory = new FakesFactory(webDb);
                webField = webFakesFactory.CreateFakeLayoutField(id);
            }

            // Act
            cache.AddLayoutValue(masterField.Item, masterField.Value);
            cache.AddLayoutValue(webField.Item, webField.Value);

            // Assert
            Assert.Equal(2, cache.InnerCache.Count);
        }

        [Fact]
        public void GetLayoutValue_WithEmptyCache_ReturnsNull()
        {
            // Arrange
            var item = MasterFakesFactory.CreateFakeItem();
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings()) {Enabled = true};
            cache.Clear();

            // Act
            var result = cache.GetLayoutValue(item);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetLayoutValue_AfterAddLayoutValue_ReturnsAddedValue()
        {
            // Arrange
            var value = "Here be ye olde layout value.";
            var item = MasterFakesFactory.CreateFakeItem(null, null, value);
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings()) {Enabled = true};
            cache.Clear();

            // Act
            cache.AddLayoutValue(item, value);
            var result = cache.GetLayoutValue(item);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void GetCacheKey_WithoutCircularReference_ReturnsKeyThatStartsWithDatabase()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings());
            var item1 = MasterFakesFactory.CreateFakeItem();
            var item2 = MasterFakesFactory.CreateFakeItem(null, null, null, null, item1.ID);

            // Act
            var result = cache.GetCacheKey(item2);

            // Assert
            Assert.True(result.StartsWith("master", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetCacheKey_WithoutCircularReference_ReturnsKeyThatEndsWithItemId()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings());
            var item1 = MasterFakesFactory.CreateFakeItem();
            var item2 = MasterFakesFactory.CreateFakeItem(null, null, null, null, item1.ID);

            // Act
            var result = cache.GetCacheKey(item2);

            // Assert
            Assert.True(result.EndsWith(item2.ID.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetCacheKey_WhenItemHasBaseLayoutReferencingSelf_ThrowsCircularReferenceException()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings());
            var id = new ID();
            var item = MasterFakesFactory.CreateFakeItem(id, null, null, null, id);

            // Act => Assert
            Assert.Throws<CircularReferenceException>(() => cache.GetCacheKey(item));
        }

        [Fact]
        public void GetCacheKey_WhenItemHasMultilevelCircularBaseLayoutReference_ThrowsCircularReferenceException()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings());
            var id = new ID();
            var baseId = new ID();
            var baseItem = MasterFakesFactory.CreateFakeItem(baseId, null, null, null, id);
            var item = MasterFakesFactory.CreateFakeItem(id, null, null, null, baseId);

            // Act => Assert
            Assert.Throws<CircularReferenceException>(() => cache.GetCacheKey(item));
        }

        [Fact]
        public void ProcessItemUpdate_WithUnrelatedItem_DoesNotRemoveEntries()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings(new[] {"master"})) {Enabled = true};
            cache.Clear();
            var count = 3;
            for (var i = 0; i < count; i++)
            {
                var field = MasterFakesFactory.CreateFakeLayoutField();
                cache.AddLayoutValue(field.Item, field.Value);
            }

            // Act
            cache.ProcessItemUpdate(MasterFakesFactory.CreateFakeItem());

            // Assert
            Assert.Equal(count, cache.InnerCache.Count);
        }

        [Fact]
        public void ProcessItemUpdate_WithItemMatchingEntryItem_RemovesEntries()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings(new[] {"master"})) {Enabled = true};
            cache.Clear();
            Field field = null;
            var count = 3;
            for (var i = 0; i < count; i++)
            {
                field = MasterFakesFactory.CreateFakeLayoutField();
                cache.AddLayoutValue(field.Item, field.Value);
            }

            // Act
            cache.ProcessItemUpdate(field.Item);

            // Assert
            Assert.True(count > cache.InnerCache.Count);
        }

        [Fact]
        public void ProcessItemUpdate_WithBaseLayoutOfItemWithEntry_RemovesEntry()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings(new[] {"master"})) {Enabled = true};
            cache.Clear();
            var baseLayoutItem = MasterFakesFactory.CreateFakeItem();
            var field = MasterFakesFactory.CreateFakeLayoutField(null, null, null, null, baseLayoutItem.ID);
            cache.AddLayoutValue(field.Item, field.Value);

            // Act
            cache.ProcessItemUpdate(baseLayoutItem);

            // Assert
            Assert.Equal(0, cache.InnerCache.Count);
        }

        [Fact]
        public void ProcessItemUpdate_WithEntriesInDifferentDatabases_OnlyRemovesEntryForMatchingDatabase()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings(new[] {"master"})) {Enabled = true};
            cache.Clear();
            var id = new ID();
            var masterField = MasterFakesFactory.CreateFakeLayoutField(id);
            Field webField;
            using (var webDb = new Db("web"))
            {
                var webFakesFactory = new FakesFactory(webDb);
                webField = webFakesFactory.CreateFakeLayoutField(id);
            }

            cache.AddLayoutValue(masterField.Item, masterField.Value);
            cache.AddLayoutValue(webField.Item, webField.Value);

            // Act
            cache.ProcessItemUpdate(masterField.Item);

            // Assert
            Assert.Null(cache.GetLayoutValue(masterField.Item));
            Assert.Equal(1, cache.InnerCache.Count);
        }

        [Fact]
        public void ProcessItemUpdate_WithEntriesForBaseLayoutChain_OnlyRemovesEntriesForDependentItems()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings(new[] {"master"})) {Enabled = true};
            cache.Clear();

            // create 2 base layout chains of 5 items each
            // save the 3rd item in the first chain as the item to pass to ProcessItemUpdate
            ID id = null;
            Item updatedItem = null;
            for (var i = 0; i < 10; i++)
            {
                var field = MasterFakesFactory.CreateFakeLayoutField(null, null, null, null, id);
                cache.AddLayoutValue(field.Item, field.Value);
                id = (i == 4) ? null : field.Item.ID;
                if (i == 2) updatedItem = field.Item;
            }

            // Act
            cache.ProcessItemUpdate(updatedItem);

            // Assert
            Assert.Equal(7, cache.InnerCache.Count);
        }

        [Fact]
        public void ProcessItemUpdate_WithStandardValuesItem_RemovesAllEntriesForMatchingDatabase()
        {
            // Arrange
            var cache = new BaseLayoutValueCache(TestUtil.CreateFakeSettings(new[] {"master"})) {Enabled = true};
            cache.Clear();
            for (var i = 0; i < 3; i++)
            {
                var masterField = MasterFakesFactory.CreateFakeLayoutField();
                cache.AddLayoutValue(masterField.Item, masterField.Value);
            }

            using (var webDb = new Db("web"))
            {
                var webFakesFactory = new FakesFactory(webDb);
                for (var i = 0; i < 3; i++)
                {
                    var webField = webFakesFactory.CreateFakeLayoutField();
                    cache.AddLayoutValue(webField.Item, webField.Value);
                }
            }

            var tid = new ID();
            MasterDb.Add(new DbTemplate("Test", tid)
            {
                Fields = {{"Title", "$name"}},
                Children = {new DbItem("__Standard Values", new ID(), tid)}
            });

            var standardValues = MasterDb.GetItem("/sitecore/templates/Test/__Standard Values");

            // Act
            cache.ProcessItemUpdate(standardValues);

            // Assert
            Assert.Equal(3, cache.InnerCache.Count);
            Assert.Equal(0, cache.InnerCache.GetCacheKeys("master:").Count);
        }
    }
}