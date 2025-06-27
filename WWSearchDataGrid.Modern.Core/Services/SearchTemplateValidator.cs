using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core.Services
{
    /// <summary>
    /// Service for validating search templates and groups
    /// </summary>
    public class SearchTemplateValidator
    {
        /// <summary>
        /// Validates whether a search group can be added
        /// </summary>
        /// <param name="searchGroups">Current search groups collection</param>
        /// <param name="allowMultipleGroups">Whether multiple groups are allowed</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateAddSearchGroup(
            ObservableCollection<SearchTemplateGroup> searchGroups,
            bool allowMultipleGroups)
        {
            // First group always allowed, additional groups only if AllowMultipleGroups is true
            if (searchGroups.Count == 0 || allowMultipleGroups)
            {
                return ValidationResult.Success();
            }

            return ValidationResult.Failure("Cannot add multiple groups when AllowMultipleGroups is false");
        }

        /// <summary>
        /// Validates whether a search template can be added to a group
        /// </summary>
        /// <param name="targetGroup">Group to add the template to</param>
        /// <param name="searchGroups">All search groups for context</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateAddSearchTemplate(
            SearchTemplateGroup targetGroup,
            ObservableCollection<SearchTemplateGroup> searchGroups)
        {
            if (targetGroup == null)
            {
                return ValidationResult.Failure("Target group cannot be null");
            }

            if (!searchGroups.Contains(targetGroup))
            {
                return ValidationResult.Failure("Target group must exist in the search groups collection");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates whether a search group can be removed
        /// </summary>
        /// <param name="groupToRemove">Group to remove</param>
        /// <param name="searchGroups">Current search groups collection</param>
        /// <param name="allowMultipleGroups">Whether multiple groups are allowed</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateRemoveSearchGroup(
            SearchTemplateGroup groupToRemove,
            ObservableCollection<SearchTemplateGroup> searchGroups,
            bool allowMultipleGroups)
        {
            if (groupToRemove == null)
            {
                return ValidationResult.Failure("Group to remove cannot be null");
            }

            if (!searchGroups.Contains(groupToRemove))
            {
                return ValidationResult.Failure("Group to remove must exist in the collection");
            }

            // If multiple groups are allowed and we have more than one group, removal is valid
            if (allowMultipleGroups && searchGroups.Count > 1)
            {
                return ValidationResult.Success();
            }

            // For the last remaining group, we should clear and reset instead of removing
            if (searchGroups.Count == 1)
            {
                return ValidationResult.Success("Will clear and reset last group instead of removing");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates whether a search template can be removed from a group
        /// </summary>
        /// <param name="templateToRemove">Template to remove</param>
        /// <param name="parentGroup">Parent group containing the template</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateRemoveSearchTemplate(
            SearchTemplate templateToRemove,
            SearchTemplateGroup parentGroup)
        {
            if (templateToRemove == null)
            {
                return ValidationResult.Failure("Template to remove cannot be null");
            }

            if (parentGroup == null)
            {
                return ValidationResult.Failure("Parent group cannot be null");
            }

            if (!parentGroup.SearchTemplates.Contains(templateToRemove))
            {
                return ValidationResult.Failure("Template must exist in the parent group");
            }

            // Always allow removal if there's more than one template
            if (parentGroup.SearchTemplates.Count > 1)
            {
                return ValidationResult.Success();
            }

            // If this is the last template, we should add a new empty one
            return ValidationResult.Success("Will add new empty template after removing the last one");
        }

        /// <summary>
        /// Validates template move operation parameters
        /// </summary>
        /// <param name="sourceGroup">Source group</param>
        /// <param name="targetGroup">Target group</param>
        /// <param name="template">Template to move</param>
        /// <param name="targetIndex">Target index</param>
        /// <param name="searchGroups">All search groups for context</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateTemplateMoveOperation(
            SearchTemplateGroup sourceGroup,
            SearchTemplateGroup targetGroup,
            SearchTemplate template,
            int targetIndex,
            ObservableCollection<SearchTemplateGroup> searchGroups)
        {
            if (sourceGroup == null)
                return ValidationResult.Failure("Source group cannot be null");

            if (targetGroup == null)
                return ValidationResult.Failure("Target group cannot be null");

            if (template == null)
                return ValidationResult.Failure("Template to move cannot be null");

            if (!searchGroups.Contains(sourceGroup))
                return ValidationResult.Failure("Source group must exist in search groups");

            if (!searchGroups.Contains(targetGroup))
                return ValidationResult.Failure("Target group must exist in search groups");

            if (!sourceGroup.SearchTemplates.Contains(template))
                return ValidationResult.Failure("Template must exist in source group");

            if (targetIndex < 0 || targetIndex > targetGroup.SearchTemplates.Count)
                return ValidationResult.Failure("Target index is out of valid range");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates search condition values based on search type
        /// </summary>
        /// <param name="searchType">Type of search being performed</param>
        /// <param name="primaryValue">Primary search value</param>
        /// <param name="secondaryValue">Secondary search value (for range operations)</param>
        /// <param name="columnDataType">Type of data in the column</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateSearchCondition(
            SearchType searchType,
            object primaryValue,
            object secondaryValue,
            ColumnDataType columnDataType)
        {
            switch (searchType)
            {
                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    if (primaryValue == null || secondaryValue == null)
                    {
                        return ValidationResult.Failure($"{searchType} requires both primary and secondary values");
                    }
                    break;

                case SearchType.Contains:
                case SearchType.DoesNotContain:
                case SearchType.StartsWith:
                case SearchType.EndsWith:
                case SearchType.Equals:
                case SearchType.NotEquals:
                case SearchType.GreaterThan:
                case SearchType.GreaterThanOrEqualTo:
                case SearchType.LessThan:
                case SearchType.LessThanOrEqualTo:
                case SearchType.IsLike:
                case SearchType.IsNotLike:
                    if (primaryValue == null)
                    {
                        return ValidationResult.Failure($"{searchType} requires a primary value");
                    }
                    break;

                case SearchType.TopN:
                case SearchType.BottomN:
                    if (primaryValue == null || !int.TryParse(primaryValue.ToString(), out int count) || count <= 0)
                    {
                        return ValidationResult.Failure($"{searchType} requires a positive integer value");
                    }
                    break;

                case SearchType.IsEmpty:
                case SearchType.IsNotEmpty:
                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                case SearchType.Today:
                case SearchType.Yesterday:
                    // These don't require values
                    break;

                default:
                    return ValidationResult.Failure($"Unknown search type: {searchType}");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Warning or informational message
        /// </summary>
        public string WarningMessage { get; private set; }

        private ValidationResult(bool isValid, string errorMessage = null, string warningMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            WarningMessage = warningMessage;
        }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success(string warningMessage = null)
        {
            return new ValidationResult(true, null, warningMessage);
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult(false, errorMessage);
        }
    }
}