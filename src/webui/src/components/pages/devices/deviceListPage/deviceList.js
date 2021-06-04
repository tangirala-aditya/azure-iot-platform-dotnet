import * as React from 'react';
import { Announced } from '@fluentui/react/lib/Announced';
import { TextField } from '@fluentui/react/lib/TextField';
import { DetailsList, DetailsListLayoutMode, Selection, DetailsRow } from '@fluentui/react/lib/DetailsList';
import { MarqueeSelection } from '@fluentui/react/lib/MarqueeSelection';
import { mergeStyles } from '@fluentui/react/lib/Styling';
//import { Link } from '@fluentui/react/lib/Link';
//import { Stack } from '@fluentui/react/lib/Stack';
/*eslint eqeqeq: ["error", "smart"]*/  
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
    Questions:
    1. How do you get the simulated flag
    2. Where is device name stored?  
    3. 
    4. 
   */
   this._columns = [    
    { key: 'column2', name: 'Device ID', fieldName: 'deviceId', onRender:this.renderDeviceIdColumn, minWidth: 100, maxWidth: 300, isResizable: true },
    { key: 'column3', name: 'Device status', fieldName: 'connectionState', minWidth: 100, maxWidth: 200, isResizable: true },
    { key: 'column5', name: 'Simulated', onRender:this.renderSimulated, fieldName: 'simulated', minWidth: 150, maxWidth: 150, isResizable: false },    
    { key: 'column6', name: 'Version', fieldName: 'properties.desired.softwareConfig.version', minWidth: 150, maxWidth: 150, isResizable: false },
    { key: 'column7', name: 'Last Connected', fieldName: 'lastActivityTime', minWidth: 100, maxWidth: 200, isResizable: true },
    { key: 'column8', name: 'Authentication Type', fieldName: 'authenticationType', minWidth: 150, maxWidth: 150, isResizable: false },
  ];

    this.state = {      
      selectionDetails: this._getSelectionDetails(),
      showFilters: false,
    };
  }

  onDeviceClick = (e) => {
    
    //const targetId = e.target.dataset.deviceid;        
    //const device = this.props.devices.find(d => {return d.deviceId === targetId});    
    this.props.onOpenDevicePage(e.target.dataset.deviceid);
    //alert(this.props.devices.length);
    //alert(this.props.onDevicePageOpen);
    //alert(e.target.dataset.deviceid);      
  }

//onClick={() => this.setState({ backgroundColor: 'red' })}

  renderDeviceIdColumn = (device) => {
    //return <span  style={{color:"#e22929"}} onClick={(device) => alert(device.deviceId)}>{device.deviceId}</span>
    return <span data-deviceid={device.deviceId} style={{color:"#e22929"}} onClick={this.onDeviceClick}>{device.deviceId}</span>
  }
  /*
  renderDeviceIdColumn(device){

    alert(this.onDeviceClick);
    //const onClick = this.onDeviceClick;
    //onClick={onClick}
    return <span  style={{color:"#e22929"}}>{device.deviceId}</span>
  }
  */

  renderSimulated(){

    let num = Math.floor(Math.random() * 10)
    
    //check if the number is even
    // eslint-disable-next-line no-use-before-define
    if(num % 2 === 0) {
      return 'true';
    }else {
      return 'false';
    }
  }

  onColumnHeaderClick  = (componentRef, innerProps) => {
      //alert(componentRef);
  }
  onRenderDetailsHeader = (componentRef, innerProps) => {      
      return componentRef;
  }
  //  onRenderDetailsHeader={this.onRenderDetailsHeader}

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

  
  render() {
    const {selectionDetails } = this.state;
    const { devices } = this.props;

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
            <Announced message={`Number of items after filter applied: ${devices.length}.`} />
        </div>
        <div style={{width:"95%"}}>
        <MarqueeSelection selection={this._selection}>
          <DetailsList
            items={devices}
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

/*
function _renderItemColumn(item, index, column) {
    const fieldContent = item[column.fieldName];
  
    switch (column.key) {

      case 'column1':
        return <Link href="#">{fieldContent}</Link>;

      default:
        return <span>{fieldContent}</span>;
    }
  }
*/

