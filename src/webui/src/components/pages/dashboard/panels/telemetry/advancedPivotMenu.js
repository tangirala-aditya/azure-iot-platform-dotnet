import React from "react";
import ReactDOM from "react-dom";
import { PivotMenu } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Pivot";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./advancedPivotMenu.module.scss"));

export class AdvancedPivotMenu extends React.Component {
    constructor(props) {
        super(props);

        this.refs = React.createRef();
        this.state = {
            prevDisable: false,
            nextDisable: false,
            showSliderIcons: false,
        };
    }

    componentDidMount() {
        this.checkButtons(this.refs.offsetWidth, this.refs.scrollWidth);
    }

    UNSAFE_componentWillReceiveProps(nextprops) {
        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        if (target) {
            setTimeout(
                () =>
                    this.setState({
                        showSliderIcons:
                            this.refs.offsetWidth - 80 <= target[0].offsetWidth,
                    }),
                100
            );
        }
    }

    checkButtons = (offsetWidthValue, scrollWidthValue) => {
        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        this.setState({
            prevDisable: this.refs.scrollLeft < 0 ? true : false,
            nextDisable:
                target[0].scrollLeft >= scrollWidthValue + 600 ? true : false,
        });
    };

    slideLeft = () => {
        const offsetWidthValue = this.refs.offsetWidth,
            scrollWidthValue = this.refs.scrollWidth;
        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        target[0].scrollLeft -= offsetWidthValue / 2;
        this.checkButtons(offsetWidthValue, scrollWidthValue);
    };

    slideRight = () => {
        const offsetWidthValue = this.refs.offsetWidth,
            scrollWidthValue = this.refs.scrollWidth;

        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        target[0].scrollLeft += offsetWidthValue / 2;
        this.checkButtons(offsetWidthValue, scrollWidthValue);
    };

    render() {
        const { showSliderIcons } = { ...this.state };
        return (
            <div
                className={css("slider-container")}
                ref={(el) => {
                    this.refs = el;
                }}
            >
                {showSliderIcons && (
                    <div
                        className={css("btnSlider", "btnSliderLeft", {
                            disableSlider: this.state.prevDisable,
                        })}
                        onClick={this.slideLeft.bind(this)}
                    >
                        {"<"}
                    </div>
                )}
                <div className={css("slider-wrapper")}>
                    <PivotMenu {...this.props} />
                </div>
                {showSliderIcons && (
                    <div
                        className={css("btnSlider", "btnSliderRight", {
                            disableSlider: this.state.nextDisable,
                        })}
                        onClick={this.slideRight.bind(this)}
                    >
                        {">"}
                    </div>
                )}
            </div>
        );
    }
}
