import * as React from 'react';
import { Icon } from '@fluentui/react/lib/Icon';
import { TextField } from '@fluentui/react/lib/TextField';
import { Stack, } from '@fluentui/react/lib/Stack';
//import { DefaultPalette } from '@fluentui/react/lib/Styling';
import {DeviceGroupLink} from './deviceGroupLink';


//import { Overlay } from '@fluentui/react';





// childrenGap: 14, padding: 14,
const verticalGapStackTokens = {
    childrenGap: 0,
    padding: 0,
};

const ChevronRight = () => <Icon iconName="ChevronRight" />;
const ChevronLeft = () => <Icon iconName="ChevronLeft" />;


const ClosedStyle = {
    width: "32px",
    height:"100%",
    backgroundColor: "rgb(247, 247, 247)",
}
const OpenStyle = {
    backgroundColor: "rgb(247, 247, 247)",
    width: "250px",
    height:"100%",
}

const DeviceGroupTitle = {
    fontSize: "16px",
    fontWeight: "600",
    padding: "10px",
}

const ChevronStyle = {
    color:"rgb(255, 0, 0)",
    position: "relative",
    float: "right",
    padding: "8px",
    
}

const FilterStyles = {
    width: "220px",    
    marginLeft: "14px",
    marginRight: "10px",
    backgroundColor: "rgb(247, 247, 247)",
    marginBottom: "14px",
}

export class DeviceGroupMenu extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            isOpen: this.props.isOpen || true,            
        };        
    }

    setIsOpen(value) {    
        this.setState({isOpen: value});
    }

    render() {

        const {isOpen} = this.state;
        const {deviceGroups, currentDeviceGroup} = this.props;        
        const menu = deviceGroups.map((item) => {
            const isSelected = (item.id === currentDeviceGroup.id)? true: false;
            return (<DeviceGroupLink deviceGroup={item} onChange={this.props.onChange} isCurrent={isSelected}/>)
        });

        return (<>
            {isOpen ? 
            (
                <div style={OpenStyle}>
                <div style={ChevronStyle} onClick={() => this.setIsOpen(!isOpen)}><ChevronLeft  /></div>
                <div style={DeviceGroupTitle}>Device Groups</div>
                <div style={FilterStyles}><TextField  placeholder="Filter groups" /></div>
                
                <Stack >      
                    <Stack tokens={verticalGapStackTokens}>
                       {menu}
                    </Stack>
                </Stack>
            </div>
            ): 
            <div style={ClosedStyle}>
                <div style={ChevronStyle} onClick={() => this.setIsOpen(!isOpen)}><ChevronRight  /></div>
            </div>
        }
    </>);
    }
}


/*

 <DeviceGroupLink name="All Items" onChange={this.onDeviceGroupChange}/>
<DeviceGroupLink name="TMicroelectronics" select={true} onChange={this.onDeviceGroupChange} />
<DeviceGroupLink name="NXP" onChange={this.onDeviceGroupChange}/>                   
<DeviceGroupLink name="Renesas" onChange={this.onDeviceGroupChange}/>
<DeviceGroupLink name="Microchip" onChange={this.onDeviceGroupChange} />

export const DeviceGroupMenu = ({ open, children }) => {
    const [isOpen, setIsOpen] = React.useState(open);
  
    return (<>
            {isOpen ? 
            (
                <div style={OpenStyle}>
                <div style={ChevronStyle} onClick={() => setIsOpen(!isOpen)}><ChevronLeft  /></div>
                <div style={DeviceGroupTitle}>Device Groups</div>
                <div style={FilterStyles}><TextField  placeholder="Filter groups" /></div>
                
                <Stack >      
                    <Stack tokens={verticalGapStackTokens}>
                        <DeviceGroupLink name="All Items"/>
                        <DeviceGroupLink name="TMicroelectronics" select={true} />
                        <DeviceGroupLink name="NXP" />                   
                        <DeviceGroupLink name="Renesas" />
                        <DeviceGroupLink name="Microchip" />
                    </Stack>
                </Stack>
            </div>
            ): 
            <div style={ClosedStyle}>
                <div style={ChevronStyle} onClick={() => setIsOpen(!isOpen)}><ChevronRight  /></div>
            </div>
        }
    </>);
};

*/

/* Search box attempt
import { SearchBox } from '@fluentui/react/lib/SearchBox'
const filterIcon = { iconName: 'Filter', backgroundColor: "rgb(247, 247, 247)",};
//const FilterIcon = () => <Icon iconName="Filter" />;
<SearchBox style={{backgroundColor: "rgb(247, 247, 247)"}} placeholder="Filter groups" iconProps={filterIcon} underlined={true} />
*/ 

/* Textbox instead of search box

const FilterStyles = {
    width: "200px",    
    marginLeft: "12px",
}


*/