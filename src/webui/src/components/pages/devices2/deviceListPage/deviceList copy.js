import * as React from 'react';
import { Announced } from '@fluentui/react/lib/Announced';
import { TextField } from '@fluentui/react/lib/TextField';
import { ContextualMenu, DirectionalHint } from '@fluentui/react/';
import { DetailsList, DetailsListLayoutMode, Selection, ColumnActionsMode } from '@fluentui/react/lib/DetailsList';
import { MarqueeSelection } from '@fluentui/react/lib/MarqueeSelection';
import { mergeStyles } from '@fluentui/react/lib/Styling';
//import { Stack } from '@fluentui/react/lib/Stack';

const exampleChildClass = mergeStyles({
  display: 'block',
  marginBottom: '10px',
});

const textFieldStyles = { root: { maxWidth: '300px' } };

export class DeviceList extends React.Component {
  
  constructor(props) {
    super(props);

    this._selection = new Selection({
      onSelectionChanged: () => this.setState({ selectionDetails: this._getSelectionDetails() }),
    });

    // Populate with items for demos.
    this._allItems = [];
    for (let i = 0; i < 200; i++) {
      this._allItems.push({
        key: i,
        name: 'Item ' + i,
        value: i,
      });
    }

    this._columns = [
      { key: 'column1', name: 'Device name', fieldName: 'name', minWidth: 100, maxWidth: 300, isResizable: true,
        onColumnContextMenu: (column, ev) => {
            this.onColumnContextMenu(column, ev);
        },
        onColumnClick: (ev, column) => {
            this.onColumnContextMenu(column, ev);
        },
        columnActionsMode: ColumnActionsMode.hasDropdown,
      },
      { key: 'column2', name: 'Device ID', fieldName: 'value', minWidth: 100, maxWidth: 300, isResizable: true, 
        onColumnContextMenu: (column, ev) => {
        this.onColumnContextMenu(column, ev);
        },
        onColumnClick: (ev, column) => {
            this.onColumnContextMenu(column, ev);
        },
        columnActionsMode: ColumnActionsMode.hasDropdown,
      },
      { key: 'column3', name: 'Device status', fieldName: 'value', minWidth: 100, maxWidth: 200, isResizable: true, 
        onColumnContextMenu: (column, ev) => {
        this.onColumnContextMenu(column, ev);
        },
        onColumnClick: (ev, column) => {
            this.onColumnContextMenu(column, ev);
        },
        columnActionsMode: ColumnActionsMode.hasDropdown,
        },
      { key: 'column4', name: 'Device type', fieldName: 'value', minWidth: 100, maxWidth: 300, isResizable: true, 
        onColumnContextMenu: (column, ev) => {
        this.onColumnContextMenu(column, ev);
        },
        onColumnClick: (ev, column) => {
            this.onColumnContextMenu(column, ev);
        },
        columnActionsMode: ColumnActionsMode.hasDropdown,
      },
      { 
          key: 'column5', name: 'Simulated', fieldName: 'value', minWidth: 150, maxWidth: 150, isResizable: false, 
        onColumnContextMenu: (column, ev) => {
        this.onColumnContextMenu(column, ev);
        },
        onColumnClick: (ev, column) => {
            this.onColumnContextMenu(column, ev);
        },
        columnActionsMode: ColumnActionsMode.hasDropdown,
        },
      { key: 'column6', name: 'Last Connected', fieldName: 'value', minWidth: 100, maxWidth: 250, isResizable: true, 
        onColumnContextMenu: (column, ev) => {
        this.onColumnContextMenu(column, ev);
        },
        onColumnClick: (ev, column) => {
            this.onColumnContextMenu(column, ev);
        },
        columnActionsMode: ColumnActionsMode.hasDropdown,
      },
    ];

    this.state = {
      items: this._allItems,
      selectionDetails: this._getSelectionDetails(),
      showFilters: false,
    };
  }

onContextualMenuDismissed = () => {
    this.setState({
        contextualMenuProps: undefined,
    });
}

onColumnContextMenu = (column, ev) => {
    if (column.columnActionsMode !== ColumnActionsMode.disabled) {
        this.setState({
            contextualMenuProps: this.getContextualMenuProps(ev, column),
        });
    }
};

getContextualMenuProps = (ev, column) => {
    const items = [
        {
            key: 'aToZ',
            name: 'A to Z',
            iconProps: { iconName: 'SortUp' },
            canCheck: true,
            checked: column.isSorted && !column.isSortedDescending,
        },
        {
            key: 'zToA',
            name: 'Z to A',
            iconProps: { iconName: 'SortDown' },
            canCheck: true,
            checked: column.isSorted && column.isSortedDescending,
        }
    ];
    return {
        items: items,
        target: ev.currentTarget,
        directionalHint: DirectionalHint.bottomLeftEdge,
        gapSpace: 10,
        isBeakVisible: true,
        onDismiss: this.onContextualMenuDismissed,
    }
}

  render() {
    const { items, selectionDetails } = this.state;

    return (
      <div>
        <div className={exampleChildClass}>{selectionDetails}</div>

        <Announced message={selectionDetails} />
        <TextField
          className={exampleChildClass}
          label="Filter by name:"
          onChange={this._onFilter}
          styles={textFieldStyles}
        />
        <Announced message={`Number of items after filter applied: ${items.length}.`} />
        <div style={{width:"99%"}}>
        <MarqueeSelection selection={this._selection}>
          <DetailsList
            items={items}
            columns={this._columns}
            setKey="set"                        
            layoutMode={DetailsListLayoutMode.justified}
            selection={this._selection}
            selectionPreservedOnEmptyClick={true}
            ariaLabelForSelectionColumn="Toggle selection"
            ariaLabelForSelectAllCheckbox="Toggle selection for all items"
            checkButtonAriaLabel="select row"
            onItemInvoked={this._onItemInvoked}
          />          
        </MarqueeSelection>
        </div>
        {this.state.contextualMenuProps && <ContextualMenu {...this.state.contextualMenuProps} />}
      </div>
    );
  }

  _getSelectionDetails() {
    const selectionCount = this._selection.getSelectedCount();

    switch (selectionCount) {
      case 0:
        return 'No items selected';
      case 1:
        return '1 item selected: ' + (this._selection.getSelection()[0]).name;
      default:
        return `${selectionCount} items selected`;
    }
  }

  _onFilter = (ev, text) => {
    this.setState({
      items: text ? this._allItems.filter(i => i.name.toLowerCase().indexOf(text) > -1) : this._allItems,
    });
  };

  _onItemInvoked = (item) => {
    alert(`Item invoked: ${item.name}`);
  };
}
