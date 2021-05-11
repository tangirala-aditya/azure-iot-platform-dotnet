import * as React from 'react';
import { Announced } from '@fluentui/react/lib/Announced';
import { TextField } from '@fluentui/react/lib/TextField';
import { DetailsList, DetailsListLayoutMode, Selection, DetailsRow } from '@fluentui/react/lib/DetailsList';
import { MarqueeSelection } from '@fluentui/react/lib/MarqueeSelection';
import { mergeStyles } from '@fluentui/react/lib/Styling';
import { Link } from '@fluentui/react/lib/Link';
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

  
    /*
    this._allItems = [];
    for (let i = 0; i < 200; i++) {
      this._allItems.push({
        key: i,
        name: 'Item ' + i,
        value: i,
      });
    }
    */
   this._allItems = _getItems();

   this._columns = [
    { key: 'column1', name: 'Device name', fieldName: 'name', minWidth: 100, maxWidth: 300, isResizable: true },
    { key: 'column2', name: 'Device ID', fieldName: 'id', minWidth: 100, maxWidth: 300, isResizable: true },
    { key: 'column3', name: 'Device status', fieldName: 'status', minWidth: 100, maxWidth: 200, isResizable: true },
    { key: 'column4', name: 'Device type', fieldName: 'type', minWidth: 100, maxWidth: 200, isResizable: true },
    { key: 'column5', name: 'Simulated', fieldName: 'simulated', minWidth: 150, maxWidth: 150, isResizable: false },
    { key: 'column6', name: 'Last Connected', fieldName: 'lastConnect', minWidth: 100, maxWidth: 200, isResizable: true },
  ];

    this.state = {
      items: this._allItems,
      selectionDetails: this._getSelectionDetails(),
      showFilters: false,
    };
  }

  onColumnHeaderClick  = (componentRef, innerProps) => {
      //alert(componentRef);
  }
  onRenderDetailsHeader = (componentRef, innerProps) => {      
      return componentRef;
  }
  //  onRenderDetailsHeader={this.onRenderDetailsHeader}

  //onRenderRow={this.onRenderRow}
  onRenderRow = (componentRef, innerProps) => {    
    //debugger;  
    const customStyles = {};
     if (componentRef) {
        if (componentRef.itemIndex % 2 === 0) {
          // Every other row renders with a different background color
          //customStyles.root = { backgroundColor: "#70403d" };
        }
        return <DetailsRow {...componentRef} styles={customStyles} />;
    }
    return <DetailsRow {...componentRef}  />;
    //return  <DetailsRow {...innerProps} styles={{color: "#70403d"}} />;
  }
  /*
   /*
     const customStyles = {};
     if (innerProps) {
        if (innerProps.itemIndex % 2 === 0) {
          // Every other row renders with a different background color
          customStyles.root = { backgroundColor: "#70403d" };
        }
        return <DetailsRow {...innerProps} styles={customStyles} />;
    }
    
    return  <DetailsRow {...innerProps} styles={{color: "#70403d"}} />;;
  */

  
  render() {
    const { items, selectionDetails } = this.state;

    return (
      <div>
        

        <div style={{display:'none'}}>
        <div className={exampleChildClass}>{selectionDetails}</div>
            <Announced message={selectionDetails} />
            <TextField
            className={exampleChildClass}
            label="Filter by name:"
            onChange={this._onFilter}
            styles={textFieldStyles}
            />
            <Announced message={`Number of items after filter applied: ${items.length}.`} />
        </div>
        <div style={{width:"95%"}}>
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
            onColumnHeaderClick={this.onColumnHeaderClick}
            onRenderRow={this.onRenderRow}
            onRenderItemColumn={_renderItemColumn}
          />          
        </MarqueeSelection>
        </div>
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


function _renderItemColumn(item, index, column) {
    const fieldContent = item[column.fieldName];
  
    switch (column.key) {

      case 'column1':
        return <Link href="#">{fieldContent}</Link>;

      default:
        return <span>{fieldContent}</span>;
    }
  }

  /*
   this._columns = [
      { key: 'column1', name: 'Device name', fieldName: 'name', minWidth: 100, maxWidth: 300, isResizable: true },
      { key: 'column2', name: 'Device ID', fieldName: 'id', minWidth: 100, maxWidth: 300, isResizable: true },
      { key: 'column3', name: 'Device status', fieldName: 'status', minWidth: 100, maxWidth: 200, isResizable: true },
      { key: 'column4', name: 'Device type', fieldName: 'type', minWidth: 100, maxWidth: 200, isResizable: true },
      { key: 'column5', name: 'Simulated', fieldName: 'simulated', minWidth: 150, maxWidth: 150, isResizable: false },
      { key: 'column6', name: 'Last Connected', fieldName: 'lastConnect', minWidth: 100, maxWidth: 200, isResizable: true },
    ];*/

    function _getItems(){
        let items = [];
        var now = new Date();

        items.push({
            key: items.length,
            name: 'AIMB-228 - 1zletaks6jgb',
            id: '1zleaks6jgb',
            status: 'Connected',
            type: 'AIMB',
            simulated: 'false',
            lastConnect: now.toDateString()
          });

          items.push({
            key: items.length,
            name: 'HoboMX100 - 1453es6jgz',
            id: '1453es6jgz',
            status: 'Connected',
            type: 'Hobo',
            simulated: 'true',
            lastConnect: now.toDateString()
          });


          items.push({
            key: items.length,
            name: 'Azure Sphere - 24rasdf8xe',
            id: '24rasdf8xe',
            status: 'Offline',
            type: 'AzSphere',
            simulated: 'true',
            lastConnect: now.toDateString()
          });

          items.push({
            key: items.length,
            name: 'Rigado Cascade 500W - v2',
            id: 'zxpb23nap',
            status: 'Connected',
            type: 'Rigado Cascade',
            simulated: 'true',
            lastConnect: now.toDateString()
          });

          items.push({
            key: items.length,
            name: 'Advantech Wise X47 - v3',
            id: '2pasnvewhip',
            status: 'Connected',
            type: 'Advantech Wise',
            simulated: 'true',
            lastConnect: now.toDateString()
          });

        return items;
    }