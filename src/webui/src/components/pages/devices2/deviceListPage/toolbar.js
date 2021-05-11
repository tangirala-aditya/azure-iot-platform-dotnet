import * as React from 'react';
import { CommandBar } from '@fluentui/react/lib/CommandBar';
import { CommandBarButton } from '@fluentui/react/lib/Button';
import { getTheme, concatStyleSets } from '@fluentui/react/lib/Styling';
import { memoizeFunction } from '@fluentui/react/lib/Utilities';


const theme = getTheme();
const itemStyles= {
  icon: { color: theme.palette.red },
  iconHovered: { color: theme.palette.redDark },
};

const getCommandBarButtonStyles = memoizeFunction(
  (originalStyles) => {
    if (!originalStyles) {
      return itemStyles;
    }

    return concatStyleSets(originalStyles, itemStyles);
  },
);

// Custom renderer for main command bar items
const CustomButton = props => {
  const buttonOnMouseClick = () => alert(`${props.text} clicked`);
  // eslint-disable-next-line react/jsx-no-bind
  return <CommandBarButton {...props} onClick={buttonOnMouseClick} styles={getCommandBarButtonStyles(props.styles)} />;
};


  const _items= [
    {
      key: 'newItem',
      text: 'New',
      iconProps: { iconName: 'Add' }
    },
    {
      key: 'getlink',
      text: 'Get Link',
      iconProps: { iconName: 'Link' },      
    },
    { key: 'download', text: 'Download', iconProps: { iconName: 'Download' }, onClick: () => console.log('Download') },
    { key: 'import', text: 'Import', iconProps: { iconName: 'Import' }, onClick: () => console.log('Import') },
    { key: 'export', text: 'Export', iconProps: { iconName: 'Export' }, onClick: () => console.log('Export') },
    
  ];
  
  const _farItems = [
    { key: 'refresh', text: 'Refresh', iconOnly: true, iconProps: { iconName: 'refresh' }, onClick: () => console.log('Share') },
    {
      key: 'columns',
      text: 'Column Options',
      // This needs an ariaLabel since it's icon-only
      ariaLabel: 'Column Options',
      iconOnly: true,
      iconProps: { iconName: 'ColumnOptions' },
      onClick: () => console.log('Column Options'),
    },
    {
      key: 'filter',
      text: 'Filter',
      ariaLabel: 'Filter',
      iconOnly: true,
      iconProps: { iconName: 'Filter' },
      onClick: () => console.log('Filter'),
    },
  ];
  
export const Toolbar = () => {

    return (
        <CommandBar
          // Custom render all buttons
          buttonAs={CustomButton}
          items={_items}          
          farItems={_farItems}
          ariaLabel="Use left and right arrow keys to navigate between commands"
        />
      );
}