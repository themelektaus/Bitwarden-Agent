
addEventListener(`focusin`, e =>
{
    if (document.querySelector(`#root-content.disabled`))
        blur()
})

function blur()
{
    document.activeElement.blur()
}


addEventListener(`keydown`, e =>
{
    if (e.altKey || e.shiftKey || e.ctrlKey)
        return

    if ([`ArrowUp`, `ArrowDown`, `PageUp`, `PageDown`].indexOf(e.key) != -1)
        e.preventDefault()

    DotNet.invokeMethodAsync(`Bitwarden Agent`, `OnKeyDown`, e.key, e.shiftKey)
})

let $visibleMenuRootItem = null

addEventListener(`mousedown`, e =>
{
    const $ = e.target

    if ($.classList.contains(`menu-root-item`))
    {
        toggleMenu($)
        return
    }

    if (!$visibleMenuRootItem)
        return

    if ($.classList.contains(`interactable`))
        return

    closeMenu()
})

addEventListener(`mouseover`, e =>
{
    if (!$visibleMenuRootItem)
        return

    const $ = e.target
    if (!$.classList.contains(`menu-root-item`))
        return

    if ($visibleMenuRootItem != $)
    {
        openMenu($)
        return
    }
})

function toggleMenu($)
{
    if ($visibleMenuRootItem)
        closeMenu()
    else
        openMenu($)
}

function openMenu($)
{
    closeMenu()
    $visibleMenuRootItem = $
    $.classList.add(`visible`)
}

function closeMenu()
{
    if (!$visibleMenuRootItem)
        return

    $visibleMenuRootItem.classList.remove(`visible`)
    $visibleMenuRootItem = null
}

function switchToInput($, key)
{
    if ($ == document.activeElement)
        return

    if (key.length == 1)
        $.value += key

    $.focus()
}

function scrollChildIntoView($, index)
{
    const $child = Array.from($.children)[index]
    if (!$child)
        return;

    $child.scrollIntoView({ block: `nearest` })
}
