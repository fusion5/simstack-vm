using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;
using System.Globalization;

namespace SimStackVM
{

    // Primitive kernel language verbs
    public enum CMD
    {
        Push0, // ( -- 0: )
        Push1, // ( -- 1: )
        Add,   // ( n1:n2: -- n1+n2: )
        Drop,  // ( x: -- )
        IfNZ   // ( x: -- ) 
    }

    public interface ITerm { }
    public class KernelCommandTerm : ITerm { 
        CMD cmd;
        public KernelCommandTerm(CMD cmd)
        {
            this.cmd = cmd;
        }
        public override string ToString()
        {
            switch (cmd)
            {
                case CMD.Add:   return "<add>";
                case CMD.Drop:  return "<drop>";
                case CMD.IfNZ:  return "<ifnz>";
                case CMD.Push0: return "<push0>";
                case CMD.Push1: return "<push1>";
                default: Debug.Assert(false); return "???";
            }
        }
    }
    public class TermReference : ITerm { 
        String term; 
        public TermReference(String term)
        {
            this.term = term;
        }

        public override string ToString()
        {
            return term;
        }
    }

    [ValueConversion(typeof(List<ITerm>), typeof(string))]
    public class DefinitionBodyToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(targetType == typeof(string));
            List<ITerm> def = (List<ITerm>)value;
            return new StringBuilder().AppendJoin(" ", def).ToString();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Stack<char> myStack = new Stack<char>(); // Stack memory
        private char rA = '\0'; // Register A
        private char rB = '\0'; // Register B
        
        public ObservableCollection<ITerm> newTermBody { get; }

        public ObservableDictionary<String, List<ITerm>> dictionary { get; }

        public MainWindow()
        {
            newTermBody = new ObservableCollection<ITerm>();
            dictionary = new ObservableDictionary<string, List<ITerm>>();
            InitializeComponent();
            // Populate the initial dictionary        
            dictionary.Add("push1", singletonKernelBody(CMD.Push1));
            dictionary.Add("push0", singletonKernelBody(CMD.Push0));
            dictionary.Add("add", singletonKernelBody(CMD.Add));
            dictionary.Add("drop", singletonKernelBody(CMD.Drop));
            dictionary.Add("ifnz", singletonKernelBody(CMD.IfNZ));

            // this.DataContext = this;

            // myModel = new MainWindowViewModel(definitions);
            Dictionary.ItemsSource = dictionary;
//            Dictionary.ItemTemplate.Template.
            
            AddTerm.ItemsSource    = dictionary;
            NewTermBody.ItemsSource = newTermBody;
            ///NewTermBody.

            updateView();
        }
        private List<ITerm> singletonKernelBody(CMD c)
        {
            List<ITerm> r = new List<ITerm>();
            r.Add(new KernelCommandTerm(c));
            return r;
        }

        private void StackPopTo(object sender, RoutedEventArgs e)
        {
            try
            {
                Button bsender = (Button)sender;
                
                switch (bsender.CommandParameter.ToString().Trim().ToLower())
                {
                    case "a":
                        {
                            rA = myStack.Pop();
                            break;
                        }
                    case "b":
                        {
                            rB = myStack.Pop();
                            break;
                        }
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Do nothing for now
            }
            
            updateView();
        }
        private void StackAdd(object sender, RoutedEventArgs e)
        {
            try
            {
                rA = myStack.Pop();
                rB = myStack.Pop();
                rA += rB;
                myStack.Push(rA);
            }
            catch (Exception ex)
            {
                // Type error-- not enough items on the stack, do nothing for now
            }

            updateView();
        }

        private void StackDrop(object sender, RoutedEventArgs e)
        {
            try
            {
                myStack.Pop();
            }
            catch (Exception ex)
            {
                // Do nothing for now
            }

            updateView();
        }
        private void StackPush(object sender, RoutedEventArgs e)
        {
            Button bsender = (Button)sender;
            switch (bsender.CommandParameter.ToString().Trim())
            {
                case "0":
                    {
                        myStack.Push((char)0x00);
                        break;
                    }
                case "1":
                    {
                        myStack.Push((char)0x01);
                        break;
                    }
                default:
                    Debug.Assert(false);
                    break;
            }
            updateView();
        }

        /*private string getDefinitionPrintout(string term, List<ITerm> body)
        {
            List<string> sbodies = body.ConvertAll(new Converter<ITerm, string>(getTermPrintout));
            return term + " = " + String.Join(" ", sbodies); 
        }
        */
        private void updateView()
        {
            // Update parameters stack view
            StackMemory.Items.Clear();
            foreach (var c in myStack)
            {
                StackMemory.Items.Add(Convert.ToByte(c).ToString("X2"));
            }
            StackMemory.Items.Add("[BOTTOM]");

            // Update the register view
            Register_A.Text = Convert.ToByte(rA).ToString("X2");
            Register_B.Text = Convert.ToByte(rB).ToString("X2");
        }

        private void AddBodyTerm(object sender, RoutedEventArgs e)
        {
            if (AddTerm.SelectedItem != null)
            {
                KeyValuePair<string, List<ITerm>> p = (KeyValuePair<string, List<ITerm>>)AddTerm.SelectedItem;
                newTermBody.Add(new TermReference(p.Key));
            }
        }

        private void AddDefinition(object sender, RoutedEventArgs e)
        {
            // Validate the input data:
            // 1. The definition name should contain a-z and 0-9 and
            // underscore and it shouldn't begin with an underscore
            Regex regex = new Regex("^[a-z0-9][a-z0-9_]+$");
            if (regex.Matches(NewTermName.Text).Count != 1)
            {
                MessageBox.Show("Invalid new term name: the new definition " +
                    "name should only contain letters a-z and digits 0-9 " +
                    "and underscore (please enter a valid name)", "Error");
                return;
            }

            // 2. The newTermBody should be non-empty
            if (newTermBody.Count == 0)
            {
                MessageBox.Show("Invalid term body: the new definition body" +
                    "should not be empty (please enter some terms)", "Error");
                return;
            }
            
            // 3. The term should not be already defined
            if (dictionary.ContainsKey(NewTermName.Text))
            {
                MessageBox.Show("The term already exists in the dictionary (" + 
                    NewTermName.Text + ")!", "Error");
            }

            dictionary.Add(NewTermName.Text, newTermBody.ToList<ITerm>());
            //myModel.definitions.Add(new KeyValuePair<string, TermList<ITerm>>(NewTermName.Text, newTermBody));

            // Reset the new term form
            newTermBody.Clear();
            NewTermName.Text = "";

            updateView();
        }
    }

    // Licensed by Daniel Cazzulino under the MIT License
    /// <summary>
    /// Provides a dictionary for use with data binding.
    /// </summary>
    /// <typeparam name="TKey">Specifies the type of the keys in this collection.</typeparam>
    /// <typeparam name="TValue">Specifies the type of the values in this collection.</typeparam>
    [DebuggerDisplay("Count={Count}")]
    public class ObservableDictionary<TKey, TValue> :
    ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>,
    INotifyCollectionChanged, INotifyPropertyChanged
    {
        readonly IDictionary<TKey, TValue> dictionary;
        /// <summary>Event raised when the collection changes.</summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged = (sender, args) => { };
        /// <summary>Event raised when a property on the collection changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };
        /// <summary>
        /// Initializes an instance of the class.
        /// </summary>
        public ObservableDictionary()
        : this(new Dictionary<TKey, TValue>())
        {
        }
        /// <summary>
        /// Initializes an instance of the class using another dictionary as
        /// the key/value store.
        /// </summary>
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }
        void AddWithNotification(KeyValuePair<TKey, TValue> item)
        {
            AddWithNotification(item.Key, item.Value);
        }
        void AddWithNotification(TKey key, TValue value)
        {
            dictionary.Add(key, value);
            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
            new KeyValuePair<TKey, TValue>(key, value)));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }
        bool RemoveWithNotification(TKey key)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value) && dictionary.Remove(key))
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                new KeyValuePair<TKey, TValue>(key, value)));
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
                PropertyChanged(this, new PropertyChangedEventArgs("Values"));
                return true;
            }
            return false;
        }
        void UpdateWithNotification(TKey key, TValue value)
        {
            TValue existing;
            if (dictionary.TryGetValue(key, out existing))
            {
                dictionary[key] = value;
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                new KeyValuePair<TKey, TValue>(key, value),
                new KeyValuePair<TKey, TValue>(key, existing)));
                PropertyChanged(this, new PropertyChangedEventArgs("Values"));
            }
            else
            {
                AddWithNotification(key, value);
            }
        }
        /// <summary>
        /// Allows derived classes to raise custom property changed events.
        /// </summary>
        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged(this, args);
        }
        #region IDictionary<TKey,TValue> Members
        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public void Add(TKey key, TValue value)
        {
            AddWithNotification(key, value);
        }
        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the key; otherwise, false.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }
        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }
        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false. This method also returns false if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </returns>
        public bool Remove(TKey key)
        {
            return RemoveWithNotification(key);
        }
        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }
        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }
        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set { UpdateWithNotification(key, value); }
        }
        #endregion
        #region ICollection<KeyValuePair<TKey,TValue>> Members
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            AddWithNotification(item);
        }
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Clear();
            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
        }
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
        }
        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Count; }
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly; }
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return RemoveWithNotification(item.Key);
        }
        #endregion
        #region IEnumerable<KeyValuePair<TKey,TValue>> Members
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).GetEnumerator();
        }
        #endregion
    }
    
}
