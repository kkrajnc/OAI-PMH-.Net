/*     This file is part of OAI-PMH .Net.
*  
*      OAI-PMH .Net is free software: you can redistribute it and/or modify
*      it under the terms of the GNU General Public License as published by
*      the Free Software Foundation, either version 3 of the License, or
*      (at your option) any later version.
*  
*      OAI-PMH .Net is distributed in the hope that it will be useful,
*      but WITHOUT ANY WARRANTY; without even the implied warranty of
*      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*      GNU General Public License for more details.
*  
*      You should have received a copy of the GNU General Public License
*      along with OAI-PMH .Net.  If not, see <http://www.gnu.org/licenses/>.
*----------------------------------------------------------------------------*/

var l;
function HoverMenu($m) {
	$m.find('li.dropdown').each(function(){
		var $li = $(this);
		$li.hover(function(){
			clearTimeout(l);
			if ($li.hasClass('usedDrop')) {
			    $li.children('ul').slideDown(100);
			    $('li.dropdown').not($li).children('ul').fadeOut(200);
			}
			else if (!$('.active-click').length) {
			    $li.addClass('active').children('ul').slideDown(100);
			    $('li.dropdown').not($li).removeClass('active').children('ul').fadeOut(200);
			}
		}, function(){
		    if ($li.hasClass('usedDrop')) {
		        l = setTimeout(function () {
		            $li.children('ul').fadeOut(200);
		        }, 150);
		    }
		    else if (!$('.active-click').length)
			{
				l = setTimeout(function(){
				    $li.removeClass('active').children('ul').fadeOut(200);
				},150);
			}
		});
		$li.click(function(e){
			clearTimeout(l);
			if ($li.hasClass('usedDrop') && $li.hasClass('active-click')) {
			    $li.children('ul').fadeOut(200);
			    $('.active-click').removeClass('active-click');
			}
			else if ($li.hasClass('usedDrop')) {
			    $li.addClass('active-click').addClass('active').children('ul').slideDown(100);
			    $('li.dropdown').not($li).removeClass('active').children('ul').fadeOut(200);
			}
			else if ($li.hasClass('active-click'))
			{
			    $li.removeClass('active').children('ul').fadeOut(200);
				$('.active-click').removeClass('active-click');
			}
			else{
				$li.addClass('active-click').addClass('active').children('ul').slideDown(100);
				$('li.dropdown').not($li).removeClass('active').children('ul').fadeOut(200);
			}
		});
	});
}
