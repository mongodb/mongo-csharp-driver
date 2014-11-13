/* Copyright 2013-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations.ElementNameValidators
{
    public class UpdateOrReplacementElementNameValidator : IElementNameValidator
    {
        // private fields
        private IElementNameValidator _chosenValidator;

        // constructors
        public UpdateOrReplacementElementNameValidator()
        {
        }

        // methods
        public IElementNameValidator GetValidatorForChildContent(string elementName)
        {
            return _chosenValidator.GetValidatorForChildContent(elementName);
        }

        public bool IsValidElementName(string elementName)
        {
            // the first elementName we see determines whether we are validating an update or a replacement document
            if (_chosenValidator == null)
            {
                if (elementName.Length > 0 && elementName[0] == '$')
                {
                    _chosenValidator = UpdateElementNameValidator.Instance; ;
                }
                else
                {
                    _chosenValidator = CollectionElementNameValidator.Instance;
                }
            }
            return _chosenValidator.IsValidElementName(elementName);
        }
    }
}
