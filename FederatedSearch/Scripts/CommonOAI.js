/*     This file is part of OAI-PMH-.Net.
*  
*      OAI-PMH-.Net is free software: you can redistribute it and/or modify
*      it under the terms of the GNU General Public License as published by
*      the Free Software Foundation, either version 3 of the License, or
*      (at your option) any later version.
*  
*      OAI-PMH-.Net is distributed in the hope that it will be useful,
*      but WITHOUT ANY WARRANTY; without even the implied warranty of
*      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*      GNU General Public License for more details.
*  
*      You should have received a copy of the GNU General Public License
*      along with OAI-PMH-.Net.  If not, see <http://www.gnu.org/licenses/>.
*----------------------------------------------------------------------------*/

// Common OAI functions

function getDataProviderSettings(tableRow) {
    var settings = {};
    $(tableRow).find('input[type="hidden"], input[type="checkbox"], input[type="text"]').each(function () {
        var self = $(this);
        var name = self.prop('name');
        var dotIndex = name.lastIndexOf('.');
        var attr = dotIndex == -1 ? name : name.substring(dotIndex + 1);
        if ((attr == 'Exclude' || attr == 'FullHarvestDelete' || attr == 'HarvestDeleteFiles') && self.prop('type') == 'hidden') {
            return true; // continue
        }

        if (self.prop('type') == 'checkbox') {
            settings[attr] = self.is(':checked');
            return true;
        }
        else if (attr == 'From' || attr == 'Until') {
            settings[attr] = self.val().replace(/ /g, '');
        }
        else {
            settings[attr] = self.val();
        }
    });
    return settings;
}

function jsonRequest(uri, data) {
    return $.ajax({
        url: uri,
        processData: false,
        type: 'post',
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        data: data
    });
}

function getDataProviderTableCells(dataProviderId) {
    return $('#dataProvidersTable > tbody > tr > td > input[type="hidden"][value="' + dataProviderId + '"]').parent('td').parent('tr').children('td');
}

function toggleCollapse(panelId) {
    $(panelId).collapse('toggle');
}

